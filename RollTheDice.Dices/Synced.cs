using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Synced : DiceBlueprint
{
	public override string ClassName => "Synced";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventWeaponReload";
			return list;
		}
	}

	public Synced(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
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
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController player = @event.Userid;
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player))
		{
			return (HookResult)0;
		}
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)player && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			CCSPlayerPawn value = item.PlayerPawn.Value;
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
			CBasePlayerWeapon val = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
			if (val != null && ((CEntityInstance)val).IsValid && val.Clip1 > 0)
			{
				val.Clip1 = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBasePlayerWeapon", "m_iClip1", 0);
			}
		}
		return (HookResult)0;
	}
}
