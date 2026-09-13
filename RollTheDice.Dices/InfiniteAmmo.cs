using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices;

public class InfiniteAmmo : DiceBlueprint
{
	public readonly Random _random = new Random();

	public override string ClassName => "InfiniteAmmo";

	public override List<string> Events
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "EventWeaponFire";
			num2++;
			span[num2] = "EventWeaponReload";
			return list;
		}
	}

	public InfiniteAmmo(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = userid.PlayerPawn;
		object obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value = playerPawn.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
				obj = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
			}
		}
		CBasePlayerWeapon val = (CBasePlayerWeapon)obj;
		if (val == null || !((CEntityInstance)val).IsValid)
		{
			return (HookResult)0;
		}
		ref int clip = ref val.Clip1;
		CBasePlayerWeaponVData vData = val.VData;
		clip = ((vData != null) ? vData.MaxClip1 : 30);
		return (HookResult)0;
	}

	public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		return (HookResult)4;
	}
}
