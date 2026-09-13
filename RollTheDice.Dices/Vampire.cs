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
/// 吸血鬼 Vampire：造成伤害吸取生命；自身 HP 低于 50% 时吸血翻倍。
/// 与 SoulEater 组合（噬魂血族）：吸血再提升。
/// </summary>
public class Vampire : DiceBlueprint
{
	public override string ClassName => "Vampire";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public Vampire(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if (DiceSynergy.HasPartner(player, "SoulEater"))
		{
			DiceSynergy.AnnounceCombo(player, "噬魂血族", "吸血量进一步提升！");
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
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		if (_players.Count == 0)
		{
			return HookResult.Continue;
		}
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		VampireConfig cfg = _config.Dices.Vampire;
		float heal = @event.DmgHealth * cfg.Lifesteal;
		CCSPlayerPawn pawn = attacker.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return HookResult.Continue;
		}
		CBaseEntity entity = pawn;
		if (entity.Health < entity.MaxHealth / 2)
		{
			heal *= cfg.LowHpBonus;
		}
		if (DiceSynergy.HasPartner(attacker, "SoulEater"))
		{
			heal *= 1.5f;
		}
		int whole = (int)Math.Round(heal);
		if (whole < 1 || entity.Health >= entity.MaxHealth)
		{
			return HookResult.Continue;
		}
		entity.Health = Math.Min(entity.Health + whole, entity.MaxHealth);
		Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
		attacker.PrintToCenterAlert($"+{whole} HP");
		return HookResult.Continue;
	}
}
