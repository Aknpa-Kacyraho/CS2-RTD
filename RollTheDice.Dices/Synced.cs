using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 心有灵犀 Synced：你换弹时，范围内最近的敌人被迫掉一个弹匣。
/// </summary>
public class Synced : DiceBlueprint
{
	public override string ClassName => "Synced";

	public override List<string> Events => new List<string> { "EventWeaponReload" };

	public Synced(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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

	public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
	{
		CCSPlayerController player = @event.Userid;
		if (player == null || !player.IsValid || !_players.Contains(player))
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn selfPawn = player.PlayerPawn?.Value;
		Vector origin = ((CBaseEntity)selfPawn)?.AbsOrigin;
		if (selfPawn == null || !selfPawn.IsValid || origin == null)
		{
			return HookResult.Continue;
		}
		float range = _config.Dices.Synced.Range;
		CCSPlayerController nearest = null;
		float best = float.MaxValue;
		foreach (CCSPlayerController other in Utilities.GetPlayers())
		{
			CCSPlayerPawn pawn = other?.PlayerPawn?.Value;
			if (other == null || !other.IsValid || ((CBasePlayerController)other).IsHLTV || other == player || pawn == null || !pawn.IsValid)
			{
				continue;
			}
			if (((CBaseEntity)other).TeamNum == ((CBaseEntity)player).TeamNum || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			Vector pos = ((CBaseEntity)pawn).AbsOrigin;
			if (pos == null)
			{
				continue;
			}
			float distance = Vectors.GetDistance(origin, pos);
			if (distance <= range && distance < best)
			{
				best = distance;
				nearest = other;
			}
		}
		if (nearest == null)
		{
			return HookResult.Continue;
		}
		CBasePlayerWeapon weapon = nearest.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value;
		if (weapon != null && weapon.IsValid && weapon.Clip1 > 0)
		{
			weapon.Clip1 = 0;
			Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1", 0);
			player.PrintToCenterAlert($"心有灵犀！{((CBasePlayerController)nearest).PlayerName} 掉弹匣");
		}
		return HookResult.Continue;
	}
}
