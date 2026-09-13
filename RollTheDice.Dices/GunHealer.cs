using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices;

/// <summary>
/// 绝命枪师 GunHealer：命中敌人回复 HP。
/// </summary>
public class GunHealer : DiceBlueprint
{
	public override string ClassName => "GunHealer";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public GunHealer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
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
		Heal(attacker, _config.Dices.GunHealer.HealPerHit);
		return HookResult.Continue;
	}

	private static void Heal(CCSPlayerController player, int amount)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || amount <= 0)
		{
			return;
		}
		CBaseEntity entity = pawn;
		if (entity.Health >= entity.MaxHealth)
		{
			return;
		}
		entity.Health = Math.Min(entity.Health + amount, entity.MaxHealth);
		Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
	}
}
