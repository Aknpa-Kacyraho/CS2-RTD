using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices;

public class ResetOnReload : DiceBlueprint
{
	public override string ClassName => "ResetOnReload";

	public override List<string> Events => new List<string> { "EventWeaponReload", "EventPlayerDeath" };

	public ResetOnReload(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
	{
		if (!_config.Dices.ResetOnReload.RefillOnReload)
		{
			return HookResult.Continue;
		}
		CCSPlayerController player = @event.Userid;
		if (player == null || !player.IsValid || !_players.Contains(player))
		{
			return HookResult.Continue;
		}
		RefillMagazine(player);
		return HookResult.Continue;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		if (!_config.Dices.ResetOnReload.RefillOnKill)
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
		RefillMagazine(attacker);
		return HookResult.Continue;
	}

	private static void RefillMagazine(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		CPlayer_WeaponServices weaponServices = pawn.WeaponServices;
		CBasePlayerWeapon weapon = weaponServices?.ActiveWeapon?.Value;
		if (weapon == null || !weapon.IsValid)
		{
			return;
		}
		CBasePlayerWeaponVData vData = weapon.VData;
		if (vData == null || vData.MaxClip1 <= 1)
		{
			return;
		}
		if (weapon.Clip1 >= vData.MaxClip1)
		{
			return;
		}
		weapon.Clip1 = vData.MaxClip1;
		Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1", 0);
	}
}
