using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Pickpocket : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Pickpocket";

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

	public Pickpocket(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
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

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)info.Attacker.Value == (CEntityInstance)null || !((CEntityInstance)info.Attacker.Value).IsValid)
		{
			return (HookResult)0;
		}
		if (((CEntityInstance)info.Attacker.Value).Index == ((CEntityInstance)entity).Index)
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
				CBasePlayerController value2 = controller2.Value;
				obj4 = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj4;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val) || (CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || val2.InGameMoneyServices == null || val.InGameMoneyServices == null)
		{
			return (HookResult)0;
		}
		if (val2.InGameMoneyServices.Account <= 0)
		{
			return (HookResult)0;
		}
		float num = _config.Dices.Pickpocket.StealPercentMin + (float)_random.NextDouble() * (_config.Dices.Pickpocket.StealPercentMax - _config.Dices.Pickpocket.StealPercentMin);
		int num2 = (int)float.Round((float)val2.InGameMoneyServices.Account * num);
		if (num2 <= 0)
		{
			return (HookResult)0;
		}
		val2.InGameMoneyServices.Account -= num2;
		Utilities.SetStateChanged((CBaseEntity)(object)val2, "CCSPlayerController", "m_pInGameMoneyServices", 0);
		val.InGameMoneyServices.Account += num2;
		Utilities.SetStateChanged((CBaseEntity)(object)val, "CCSPlayerController", "m_pInGameMoneyServices", 0);
		val.PrintToCenterAlert($"\ud83e\udd11 +${num2}!");
		val2.PrintToCenterAlert($"\ud83d\ude31 -${num2}!");
		return (HookResult)0;
	}
}
