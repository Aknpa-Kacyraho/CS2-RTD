using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Guillotine : DiceBlueprint
{
	public override string ClassName => "Guillotine";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public Guillotine(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info == null || info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		CCSPlayerController victim = ResolvePlayer(entity);
		if (attacker == null || victim == null || attacker == victim)
		{
			return HookResult.Continue;
		}
		if (!_players.Contains(attacker) || ((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (victimPawn == null || !victimPawn.IsValid || victimPawn.LifeState != 0 || victimPawn.MaxHealth <= 0)
		{
			return HookResult.Continue;
		}
		if ((float)victimPawn.Health / (float)victimPawn.MaxHealth >= _config.Dices.Guillotine.ExecuteHpThreshold)
		{
			return HookResult.Continue;
		}
		info.Damage = 1000000f;
		Server.NextFrame(delegate
		{
			if (victim == null || !victim.IsValid)
			{
				return;
			}
			CCSPlayerPawn pawn = victim.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || pawn.LifeState != 0 || pawn.Health <= 0)
			{
				return;
			}
			pawn.Health = 0;
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			try
			{
				pawn.CommitSuicide(false, true);
			}
			catch
			{
			}
		});
		return HookResult.Changed;
	}

	private static CCSPlayerController ResolvePlayer(CBaseEntity entity)
	{
		if (entity == null)
		{
			return null;
		}
		CCSPlayerPawn pawn = ((NativeObject)entity).As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return null;
		}
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null || controller.Value == null)
		{
			return null;
		}
		return ((NativeObject)controller.Value).As<CCSPlayerController>();
	}
}
