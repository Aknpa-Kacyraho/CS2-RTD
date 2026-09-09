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

public class PistolMaster : DiceBlueprint
{
	private bool _comboActive;

	public override string ClassName => "PistolMaster";

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

	public PistolMaster(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Disarm");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "缴械大师", "手枪倍率2→3 缴械率翻倍");
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
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)info.Attacker.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)info.Attacker.Value).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		CCSPlayerPawn value2 = val.PlayerPawn.Value;
		object obj3;
		if (value2 == null)
		{
			obj3 = null;
		}
		else
		{
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value2).WeaponServices;
			obj3 = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
		}
		if ((CEntityInstance)obj3 == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		string text = ((CEntityInstance)((CBasePlayerPawn)value2).WeaponServices.ActiveWeapon.Value).DesignerName.ToLower();
		if (text == null)
		{
			return (HookResult)0;
		}
		bool flag = text.Contains("pistol") || text.Contains("deagle") || text.Contains("elite");
		bool flag2 = text.Contains("revolver");
		if (flag2 && !text.Contains("elite"))
		{
			return (HookResult)0;
		}
		if (!flag | flag2)
		{
			return (HookResult)0;
		}
		float num = _config.Dices.PistolMaster.DamageMultiplier;
		if (DiceSynergy.HasPartner(val, "Disarm"))
		{
			num++;
		}
		info.Damage *= num;
		return (HookResult)1;
	}
}
