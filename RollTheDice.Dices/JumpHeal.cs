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
/// 跳跳糖 JumpHeal：每次落地回复 HP。
/// 与 Regeneration 组合（生命律动）：回血量翻倍。
/// </summary>
public class JumpHeal : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, bool> _wasOnGround = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "JumpHeal";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public JumpHeal(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_wasOnGround[player] = true;
		if (DiceSynergy.HasPartner(player, "Regeneration"))
		{
			DiceSynergy.AnnounceCombo(player, "生命律动", "落地回血量翻倍，且脱战判定减半！");
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
		_wasOnGround.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_wasOnGround.Clear();
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
		JumpHealConfig cfg = _config.Dices.JumpHeal;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			CBaseEntity entity = pawn;
			bool onGround = (entity.Flags & 1u) != 0;
			bool was = (_wasOnGround.TryGetValue(player, out bool w) ? w : true);
			_wasOnGround[player] = onGround;
			if (was || !onGround || entity.Health >= entity.MaxHealth)
			{
				continue;
			}
			int heal = cfg.HealPerLand;
			if (DiceSynergy.HasPartner(player, "Regeneration"))
			{
				heal *= 2;
			}
			entity.Health = Math.Min(entity.Health + heal, entity.MaxHealth);
			Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
			player.PrintToCenterAlert($"🍬 +{heal} HP");
		}
	}
}
