using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 资本家 Capitalist：存活时每 interval 秒产生利息收入，单回合累计有上限（死亡不清，回合重置）。
/// 与 Bounty 组合（赏金猎人）：利息翻倍。
/// </summary>
public class Capitalist : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextTick = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _earned = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Capitalist";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Capitalist(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
		{
			return;
		}
		_players.Add(player);
		_nextTick[player] = Server.CurrentTime + _config.Dices.Capitalist.Interval;
		_earned[player] = 0;
		if (DiceSynergy.HasPartner(player, "Bounty"))
		{
			DiceSynergy.AnnounceCombo(player, "赏金猎人", "资本利息翻倍！");
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_nextTick.Remove(player);
		_earned.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_nextTick.Clear();
		_earned.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		CapitalistConfig cfg = _config.Dices.Capitalist;
		float now = Server.CurrentTime;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (player == null || !player.IsValid || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			if (!_nextTick.TryGetValue(player, out float next) || now < next)
			{
				continue;
			}
			_nextTick[player] = now + cfg.Interval;
			int earned = _earned.TryGetValue(player, out int e) ? e : 0;
			if (earned >= cfg.MaxTotal || player.InGameMoneyServices == null)
			{
				continue;
			}
			int amount = Math.Min(cfg.AmountPerTick, cfg.MaxTotal - earned);
			if (DiceSynergy.HasPartner(player, "Bounty"))
			{
				amount *= 2;
			}
			player.InGameMoneyServices.Account += amount;
			Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices", 0);
			_earned[player] = earned + amount;
			player.PrintToCenterAlert($"💰 资本利息 +${amount}");
		}
	}
}
