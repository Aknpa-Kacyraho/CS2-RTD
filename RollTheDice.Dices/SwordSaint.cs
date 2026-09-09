using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class SwordSaint : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, CBasePlayerWeapon?> _previousWeapon = new Dictionary<CCSPlayerController, CBasePlayerWeapon>();

	public override string ClassName => "SwordSaint";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public SwordSaint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Cutter");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "剑刃风暴", "剑刃风暴联动生效！");
			}
			_previousWeapon[player] = null;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
		_players.Remove(player);
		_previousWeapon.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_previousWeapon.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
				{
					continue;
				}
				CCSPlayerPawn value = item.PlayerPawn.Value;
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
				CBasePlayerWeapon val = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
				if (!_previousWeapon.TryGetValue(item, out CBasePlayerWeapon value2) || (CEntityInstance)(object)value2 != (CEntityInstance)(object)val)
				{
					_previousWeapon[item] = val;
					if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).DesignerName != null && ((CEntityInstance)val).DesignerName.Contains("knife"))
					{
						((CBaseModelEntity)value).Render = Color.FromArgb(255, 100, 200, 255);
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
					}
					else
					{
						((CBaseModelEntity)value).Render = Color.FromArgb(255, 255, 255, 255);
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
					}
				}
			}
			catch
			{
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
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
		string designerName = ((CEntityInstance)((CBasePlayerPawn)value2).WeaponServices.ActiveWeapon.Value).DesignerName;
		if (designerName == null || !designerName.Contains("knife"))
		{
			return (HookResult)0;
		}
		if (((uint)info.BitsDamageType & 2u) != 0)
		{
			info.Damage = 0f;
			if (DiceSynergy.HasPartner(val, "Cutter") && (CEntityInstance)(object)info.Attacker?.Value != (CEntityInstance)null)
			{
				CCSPlayerPawn obj4 = ((NativeObject)info.Attacker.Value).As<CCSPlayerPawn>();
				object obj5;
				if (obj4 == null)
				{
					obj5 = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
					if (controller2 == null)
					{
						obj5 = null;
					}
					else
					{
						CBasePlayerController value3 = controller2.Value;
						obj5 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
					}
				}
				CCSPlayerController val2 = (CCSPlayerController)obj5;
				if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid && (CEntityInstance)(object)val2.PlayerPawn?.Value != (CEntityInstance)null)
				{
					val2.PlayerPawn.Value.VelocityModifier = 0.01f;
					Utilities.SetStateChanged((CBaseEntity)(object)val2.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					CCSPlayerController capturedAttacker = val2;
					new Timer(0.5f, (Action)delegate
					{
						CCSPlayerController obj6 = capturedAttacker;
						if ((CEntityInstance)(object)((obj6 == null) ? null : obj6.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)capturedAttacker.PlayerPawn.Value).IsValid)
						{
							capturedAttacker.PlayerPawn.Value.VelocityModifier = 1f;
							Utilities.SetStateChanged((CBaseEntity)(object)capturedAttacker.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
						}
					}, (TimerFlags?)null);
				}
			}
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
