using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DeathKnightComplete : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalArmor = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _lastHealTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _startMaxHP = new Dictionary<CCSPlayerController, int>();

	public static readonly HashSet<ulong> DeniedNextRound = new HashSet<ulong>();

	public override string ClassName => "DeathKnightComplete";

	public override bool CanBeDrawn => false;

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

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

	public DeathKnightComplete(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			_originalArmor[player] = value.ArmorValue;
			((CBaseEntity)value).MaxHealth = _config.Dices.DeathKnightComplete.BonusHP;
			((CBaseEntity)value).Health = _config.Dices.DeathKnightComplete.BonusHP;
			value.ArmorValue = _config.Dices.DeathKnightComplete.BonusArmor;
			_startMaxHP[player] = ((CBaseEntity)value).MaxHealth;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			_players.Add(player);
			_lastHealTime[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageReductionManager.Unregister(player, "DeathKnightComplete");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (_originalMaxHealth.TryGetValue(player, out var value2))
			{
				((CBaseEntity)value).MaxHealth = value2;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				_originalMaxHealth.Remove(player);
			}
			if (_originalArmor.TryGetValue(player, out var value3))
			{
				value.ArmorValue = Math.Min(value.ArmorValue, value3);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				_originalArmor.Remove(player);
			}
		}
		_players.Remove(player);
		_lastHealTime.Remove(player);
		_startMaxHP.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
		_originalArmor.Clear();
		_lastHealTime.Clear();
		_startMaxHP.Clear();
		DeniedNextRound.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
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
		if (!_startMaxHP.TryGetValue(val, out var value2) || value2 <= 0)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn value3 = val.PlayerPawn.Value;
		int num = value2 - ((CBaseEntity)value3).Health;
		if (num < 0)
		{
			num = 0;
		}
		float initialDamageReduction = _config.Dices.DeathKnightComplete.InitialDamageReduction;
		float num2 = (float)num / (float)value2;
		float num3 = initialDamageReduction + num2 * (1f - initialDamageReduction);
		float maxDamageReduction = _config.Dices.DeathKnightComplete.MaxDamageReduction;
		if (num3 > maxDamageReduction)
		{
			num3 = maxDamageReduction;
		}
		info.Damage = (int)(info.Damage * (1f - num3));
		return (HookResult)1;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			bool flag = false;
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)item.PlayerPawn.Value).WeaponServices;
			object obj;
			if (weaponServices == null)
			{
				obj = null;
			}
			else
			{
				CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
				if (activeWeapon == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerWeapon value = activeWeapon.Value;
					obj = ((value != null) ? ((CEntityInstance)value).DesignerName : null);
				}
			}
			string text = (string)obj;
			if (text != null && text.Contains("knife"))
			{
				flag = true;
			}
			if (flag && _lastHealTime.TryGetValue(item, out var value2) && num - value2 >= 1f)
			{
				_lastHealTime[item] = num;
				CCSPlayerPawn value3 = item.PlayerPawn.Value;
				((CBaseEntity)value3).Health = Math.Min(((CBaseEntity)value3).Health + _config.Dices.DeathKnightComplete.KnifeHealPerSec, ((CBaseEntity)value3).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if (!_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = attacker.PlayerPawn;
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
				if (weaponServices == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
					if (activeWeapon == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerWeapon value2 = activeWeapon.Value;
						obj = ((value2 != null) ? ((CEntityInstance)value2).DesignerName : null);
					}
				}
			}
		}
		string text = (string)obj;
		if (text != null && text.Contains("knife"))
		{
			DeniedNextRound.Add(((CBasePlayerController)userid).SteamID);
			userid.PrintToCenterAlert("☠ 被死亡骑士的霜之哀伤斩杀！下回合无法获得骰子！");
		}
		return (HookResult)0;
	}
}
