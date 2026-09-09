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

public class Titanfall : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, float> _releaseTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, bool> _released = new Dictionary<CCSPlayerController, bool>();

	private readonly Dictionary<CCSPlayerController, float> _lockdownStart = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _initialHP = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _initialArmor = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Titanfall";

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

	public Titanfall(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Gargoyle");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "泰坦神像", "泰坦提前15s觉醒");
			}
			float num = (_comboActive ? Math.Max(_config.Dices.Titanfall.LockdownSeconds - 15f, 5f) : _config.Dices.Titanfall.LockdownSeconds);
			float num2 = Server.CurrentTime;
			_releaseTime[player] = num2 + num;
			_lockdownStart[player] = num2;
			_released[player] = false;
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_initialHP[player] = ((CBaseEntity)value).Health;
			_initialArmor[player] = Math.Max(value.ArmorValue, 100);
			value.ArmorValue = Math.Max(value.ArmorValue, 100);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			player.GiveNamedItem("item_assaultsuit");
			MoveLockManager.Lock(player, "Titanfall");
			player.PrintToCenterAlert($"\ud83d\ude80 泰坦陨落！{num}s 后觉醒...");
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		MoveLockManager.Unlock(player, "Titanfall");
		DamageBonusManager.Unregister(player, "Titanfall");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
		_players.Remove(player);
		_releaseTime.Remove(player);
		_released.Remove(player);
		_lockdownStart.Remove(player);
		_initialHP.Remove(player);
		_initialArmor.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			MoveLockManager.Unlock(item, "Titanfall");
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				item.PlayerPawn.Value.VelocityModifier = 1f;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		_players.Clear();
		_releaseTime.Clear();
		_released.Clear();
		_lockdownStart.Clear();
		_initialHP.Clear();
		_initialArmor.Clear();
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
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid)
				{
					continue;
				}
				CCSPlayerPawn value = item.PlayerPawn.Value;
				if (((CBaseEntity)value).LifeState != 0 || !_releaseTime.TryGetValue(item, out var value2) || !_released.TryGetValue(item, out var value3))
				{
					continue;
				}
				if (!value3)
				{
					MoveLockManager.Lock(item, "Titanfall");
					float num2 = value2 - num;
					float valueOrDefault = _lockdownStart.GetValueOrDefault(item, num);
					float num3 = value2 - valueOrDefault;
					float num4 = ((num3 > 0f) ? Math.Min((num - valueOrDefault) / num3, 1f) : 1f);
					int titanHP = _config.Dices.Titanfall.TitanHP;
					int titanArmor = _config.Dices.Titanfall.TitanArmor;
					int valueOrDefault2 = _initialHP.GetValueOrDefault(item, 100);
					int valueOrDefault3 = _initialArmor.GetValueOrDefault(item, 0);
					((CBaseEntity)value).MaxHealth = (int)float.Round((float)valueOrDefault2 + (float)(titanHP - valueOrDefault2) * num4);
					((CBaseEntity)value).Health = ((CBaseEntity)value).MaxHealth;
					value.ArmorValue = (int)float.Round((float)valueOrDefault3 + (float)(titanArmor - valueOrDefault3) * num4);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
					if (num2 <= 0f)
					{
						_released[item] = true;
						MoveLockManager.Unlock(item, "Titanfall");
						((CBaseEntity)value).MaxHealth = titanHP;
						((CBaseEntity)value).Health = titanHP;
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
						value.VelocityModifier = 1.5f;
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
						DamageBonusManager.Register(item, "Titanfall", _config.Dices.Titanfall.DamageMultiplier - 1f);
						item.PrintToCenterAlert("⚡ 泰坦觉醒！+50%伤害 +50%移速！");
						Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Titanfall_awaken"].Value.Replace("{playerName}", ((CBasePlayerController)item).PlayerName));
					}
					else if (Server.TickCount % 64 == 0)
					{
						item.PrintToCenterAlert($"\ud83d\ude80 泰坦陨落中... HP:{((CBaseEntity)value).Health}/{((CBaseEntity)value).MaxHealth} 甲:{value.ArmorValue} {Math.Ceiling(num2)}s");
					}
				}
				else if (value.VelocityModifier < 1.4f)
				{
					value.VelocityModifier = 1.5f;
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
			catch
			{
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		if (!_released.TryGetValue(val, out var value3) || !value3)
		{
			return (HookResult)0;
		}
		if (DamageBonusManager.IsHighest(val, "Titanfall"))
		{
			float effective = DamageBonusManager.GetEffective(val);
			info.Damage = (int)(info.Damage * (1f + effective));
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
