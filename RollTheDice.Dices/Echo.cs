using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 回音 Echo：命中敌人 delay_seconds 后，对同一目标追加一次 damage_fraction 的回声伤害。
/// </summary>
public class Echo : DiceBlueprint
{
	public override string ClassName => "Echo";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public Echo(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker == (CEntityInstance)(object)victim || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return (HookResult)0;
		}
		float damage = @event.DmgHealth;
		if (damage <= 0f)
		{
			return (HookResult)0;
		}
		int echo = (int)Math.Round(Math.Min(damage * _config.Dices.Echo.DamageFraction, _config.Dices.Echo.MaxDamage));
		if (echo < 1)
		{
			return (HookResult)0;
		}
		ulong victimId = ((CBasePlayerController)victim).SteamID;
		new Timer(_config.Dices.Echo.DelaySeconds, (Action)delegate
		{
			ApplyEcho(victimId, echo);
		}, (TimerFlags?)null);
		return (HookResult)0;
	}

	private void ApplyEcho(ulong victimId, int echo)
	{
		CCSPlayerController victim = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == victimId);
		if (victim == null || (CEntityInstance)(object)victim.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)victim.PlayerPawn.Value).IsValid || ((CBaseEntity)victim.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		CCSPlayerPawn pawn = victim.PlayerPawn.Value;
		((CBaseEntity)pawn).Health -= echo;
		Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iHealth", 0);
		if (((CBaseEntity)pawn).Health <= 0)
		{
			try
			{
				((CBasePlayerPawn)pawn).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)pawn).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}
}
