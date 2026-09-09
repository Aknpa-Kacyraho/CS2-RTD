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

public class Disarm : DiceBlueprint
{
	private bool _comboActive;

	public override string ClassName => "Disarm";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public Disarm(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "PistolMaster");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "缴械大师", "缴械率翻倍");
			}
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

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CBaseEntity value = attacker.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn obj2 = ((NativeObject)value).As<CCSPlayerPawn>();
				if (obj2 == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj2).Controller;
					if (controller == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerController value2 = controller.Value;
						obj = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj;
		CCSPlayerPawn obj3 = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj4;
		if (obj3 == null)
		{
			obj4 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj3).Controller;
			if (controller2 == null)
			{
				obj4 = null;
			}
			else
			{
				CBasePlayerController value3 = controller2.Value;
				obj4 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj4;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val) || (CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val2 == (CEntityInstance)(object)val || ((CBaseEntity)val2).TeamNum == ((CBaseEntity)val).TeamNum)
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = val2.PlayerPawn;
		object obj5;
		if (playerPawn == null)
		{
			obj5 = null;
		}
		else
		{
			CCSPlayerPawn value4 = playerPawn.Value;
			obj5 = ((value4 != null) ? ((CBasePlayerPawn)value4).WeaponServices : null);
		}
		if (obj5 == null)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn value5 = val2.PlayerPawn.Value;
		CHandle<CBasePlayerWeapon> activeWeapon = ((CBasePlayerPawn)value5).WeaponServices.ActiveWeapon;
		if ((CEntityInstance)(object)activeWeapon?.Value != (CEntityInstance)null && ((CEntityInstance)activeWeapon.Value).IsValid)
		{
			string text = ((CEntityInstance)activeWeapon.Value).DesignerName ?? "";
			if (!text.Contains("knife") && !text.Contains("c4"))
			{
				val2.DropActiveWeapon();
				val2.PrintToCenterAlert("\ud83d\udd2b 武器被打掉了!");
			}
		}
		return (HookResult)0;
	}
}
