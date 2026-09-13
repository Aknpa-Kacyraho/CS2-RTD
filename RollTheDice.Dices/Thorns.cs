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

public class Thorns : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _reflectDamage = new Dictionary<CCSPlayerController, int>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Thorns";

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

	public Thorns(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_comboActive = DiceSynergy.HasPartner(player, "GuardianAngel") || DiceSynergy.HasPartner(player, "MagneticPulse");
			int value = _random.Next(_config.Dices.Thorns.ReflectDamageMin, _config.Dices.Thorns.ReflectDamageMax + 1) + (_comboActive ? 10 : 0);
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "GuardianAngel"))
			{
				DiceSynergy.AnnounceCombo(player, "圣光荆棘", "圣光荆棘联动生效！");
			}
			if (DiceSynergy.HasPartner(player, "MagneticPulse"))
			{
				DiceSynergy.AnnounceCombo(player, "磁力荆棘", "魔镜反弹+磁力脉冲！双重重压！");
			}
			_reflectDamage[player] = value;
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"damage",
					value.ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_reflectDamage.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_reflectDamage.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid || (CEntityInstance)(object)info.Attacker.Value == (CEntityInstance)null || !((CEntityInstance)info.Attacker.Value).IsValid)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
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
		CCSPlayerController victim = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid || !_reflectDamage.TryGetValue(victim, out var _))
		{
			return (HookResult)0;
		}
		if (((CEntityInstance)info.Attacker.Value).Index == ((CEntityInstance)entity).Index)
		{
			return (HookResult)0;
		}
		int reflectedDmg = (int)info.Damage;
		Server.NextFrame((Action)delegate
		{
			CBaseEntity value3 = info.Attacker.Value;
			CCSPlayerPawn val = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerPawn>() : null);
			if (!((CEntityInstance)(object)val == (CEntityInstance)null) && ((CEntityInstance)val).IsValid && ((CBaseEntity)val).LifeState == 0)
			{
				((CBaseEntity)val).Health -= reflectedDmg;
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iHealth", 0);
				CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)val).Controller;
				object obj3;
				if (controller2 == null)
				{
					obj3 = null;
				}
				else
				{
					CBasePlayerController value4 = controller2.Value;
					obj3 = ((value4 != null) ? ((NativeObject)value4).As<CCSPlayerController>() : null);
				}
				CCSPlayerController val2 = (CCSPlayerController)obj3;
				if (val2 != null)
				{
					val2.PrintToCenterAlert($"\ud83e\ude9e 魔镜反弹 -{reflectedDmg}!");
				}
				CCSPlayerController obj4 = victim;
				if (obj4 != null)
				{
					obj4.PrintToCenterAlert($"\ud83e\ude9e 反弹 {reflectedDmg} 伤害!");
				}
			}
		});
		return (HookResult)0;
	}
}
