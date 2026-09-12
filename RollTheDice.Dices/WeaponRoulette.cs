using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class WeaponRoulette : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private static readonly string[] _weaponList = new string[34]
	{
		"weapon_ak47", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_aug", "weapon_sg556", "weapon_galilar", "weapon_famas", "weapon_awp", "weapon_ssg08", "weapon_scar20",
		"weapon_g3sg1", "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249", "weapon_negev", "weapon_mp9", "weapon_mac10", "weapon_mp7",
		"weapon_mp5sd", "weapon_ump45", "weapon_p90", "weapon_bizon", "weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_glock", "weapon_hkp2000", "weapon_p250",
		"weapon_tec9", "weapon_usp_silencer", "weapon_cz75a", "weapon_revolver"
	};

	public override string ClassName => "WeaponRoulette";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventWeaponFire";
			return list;
		}
	}

	public WeaponRoulette(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			DamageBonusManager.Register(player, "WeaponRoulette", 0.3f);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageBonusManager.Unregister(player, "WeaponRoulette");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players)
		{
			DamageBonusManager.Unregister(item, "WeaponRoulette");
		}
		_players.Clear();
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController player = @event.Userid;
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && ((CBaseEntity)player.PlayerPawn.Value).LifeState == 0)
			{
				CCSPlayerPawn value = player.PlayerPawn.Value;
				int armorValue = value.ArmorValue;
				bool flag = ((CBasePlayerPawn)value).ItemServices != null && new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value).ItemServices).Handle).HasHelmet;
				player.RemoveWeapons();
				string text = _weaponList[_random.Next(_weaponList.Length)];
				player.GiveNamedItem(text);
				player.GiveNamedItem("weapon_knife");
				value.ArmorValue = armorValue;
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				if (flag && ((CBasePlayerPawn)value).ItemServices != null)
				{
					new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value).ItemServices).Handle).HasHelmet = true;
				}
			}
		});
		return (HookResult)0;
	}
}
