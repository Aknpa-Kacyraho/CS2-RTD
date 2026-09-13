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
/// 银行 Bank：每间隔给队内最穷的若干名队友注资（精准支援）。
/// 与 Miser 组合（资本要塞）：注资金额翻倍。
/// </summary>
public class Bank : DiceBlueprint
{
	private float _nextPayout;

	public override string ClassName => "Bank";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Bank(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if (DiceSynergy.HasPartner(player, "Miser"))
		{
			DiceSynergy.AnnounceCombo(player, "资本要塞", "注资金额翻倍！");
		}
		if (_nextPayout == 0f)
		{
			_nextPayout = Server.CurrentTime + _config.Dices.Bank.Interval;
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
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_nextPayout = 0f;
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
		float now = Server.CurrentTime;
		if (now < _nextPayout)
		{
			return;
		}
		_nextPayout = now + _config.Dices.Bank.Interval;
		BankConfig cfg = _config.Dices.Bank;
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if (holder == null || !holder.IsValid)
			{
				continue;
			}
			List<CCSPlayerController> teammates = Utilities.GetPlayers()
				.Where((CCSPlayerController p) => p != null && p.IsValid && !p.IsHLTV && p != holder && ((CBaseEntity)p).TeamNum == ((CBaseEntity)holder).TeamNum && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0)
				.OrderBy((CCSPlayerController p) => ((CBasePlayerController)p).SteamID)
				.ToList();
			if (teammates.Count == 0)
			{
				continue;
			}
			teammates = teammates.OrderBy((CCSPlayerController p) => p.InGameMoneyServices?.Account ?? 0).ToList();
			int count = Math.Min(cfg.TeammatesCount, teammates.Count);
			int amount = cfg.Amount;
			if (DiceSynergy.HasPartner(holder, "Miser"))
			{
				amount *= 2;
			}
			for (int i = 0; i < count; i++)
			{
				CCSPlayerController target = teammates[i];
				if (target.InGameMoneyServices == null)
				{
					continue;
				}
				target.InGameMoneyServices.Account += amount;
				Utilities.SetStateChanged(target, "CCSPlayerController", "m_pInGameMoneyServices", 0);
				target.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Bank_payout"].Value.Replace("{amount}", "+" + amount).Replace("{name}", ((CBasePlayerController)target).PlayerName));
			}
		}
	}
}
