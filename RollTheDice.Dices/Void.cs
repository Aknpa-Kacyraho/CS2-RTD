using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Void : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _voidEndTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _cooldownEnd = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _lastVoidCountdown = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Void";

	public override List<string> Listeners
	{
		get
		{
			int num = 3;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerButtonsChanged";
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
			span[index] = "EventWeaponFire";
			return list;
		}
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		float value;
		return _cooldownEnd.TryGetValue(player, out value) ? Math.Max(0f, value - Server.CurrentTime) : 0f;
	}

	public Void(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_voidEndTime[player] = 0f;
			_cooldownEnd[player] = 0f;
			_lastVoidCountdown[player] = -1;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83c\udf11 按E键遁入虚无！无敌+飞行+隐身5s！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DeactivateVoid(player);
		_players.Remove(player);
		_voidEndTime.Remove(player);
		_cooldownEnd.Remove(player);
		_lastVoidCountdown.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DeactivateVoid(item);
		}
		_players.Clear();
		_voidEndTime.Clear();
		_cooldownEnd.Clear();
		_lastVoidCountdown.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void ActivateVoid(CCSPlayerController player)
	{
		CCSPlayerPawn val = player.PlayerPawn?.Value;
		if (val != null && ((CEntityInstance)val).IsValid)
		{
			float num = Server.CurrentTime;
			_voidEndTime[player] = num + _config.Dices.Void.Duration;
			_cooldownEnd[player] = num + _config.Dices.Void.Cooldown;
			((CBaseEntity)val).MoveType = (MoveType_t)7;
			Schema.SetSchemaValue<int>(((NativeEntity)val).Handle, "CBaseEntity", "m_nActualMoveType", 7);
			((CBaseModelEntity)val).Render = Color.FromArgb(20, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseModelEntity", "m_clrRender", 0);
			player.PrintToCenterAlert("\ud83c\udf11 遁入虚无！5s无敌+飞行+隐身！");
		}
	}

	private void DeactivateVoid(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid)
		{
			CCSPlayerPawn val = player.PlayerPawn?.Value;
			if (val != null && ((CEntityInstance)val).IsValid && ((CBaseEntity)val).LifeState == 0)
			{
				((CBaseEntity)val).MoveType = (MoveType_t)2;
				Schema.SetSchemaValue<int>(((NativeEntity)val).Handle, "CBaseEntity", "m_nActualMoveType", 2);
				((CBaseModelEntity)val).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseModelEntity", "m_clrRender", 0);
			}
		}
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count != 0 && !((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _players.Contains(player) && ((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && ((CBaseEntity)player.PlayerPawn.Value).LifeState == 0)
		{
			float num = Server.CurrentTime;
			if ((!_cooldownEnd.TryGetValue(player, out var value) || !(num < value)) && (!_voidEndTime.TryGetValue(player, out var value2) || !(num < value2)))
			{
				ActivateVoid(player);
			}
		}
	}

	public void OnTick()
	{
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				continue;
			}
			if (_voidEndTime.TryGetValue(item, out var value) && num >= value && value > 0f)
			{
				_voidEndTime[item] = 0f;
				DeactivateVoid(item);
				item.PrintToCenterAlert("☀\ufe0f 虚无消散！回到现实！");
			}
			if (_voidEndTime.TryGetValue(item, out var value2) && num < value2 && value2 > 0f)
			{
				CCSPlayerPawn value3 = item.PlayerPawn.Value;
				if ((int)((CBaseEntity)value3).MoveType != 7)
				{
					((CBaseEntity)value3).MoveType = (MoveType_t)7;
					Schema.SetSchemaValue<int>(((NativeEntity)value3).Handle, "CBaseEntity", "m_nActualMoveType", 7);
				}
				int num2 = (int)Math.Ceiling(value2 - num);
				int valueOrDefault = _lastVoidCountdown.GetValueOrDefault(item, -1);
				if (num2 > 0 && num2 != valueOrDefault)
				{
					_lastVoidCountdown[item] = num2;
					item.PrintToCenterAlert($"\ud83c\udf11 虚无！{num2}秒剩余");
				}
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		float num = Server.CurrentTime;
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
		if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid && _players.Contains(val) && _voidEndTime.TryGetValue(val, out var value2) && num < value2)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj3;
		if (attacker == null)
		{
			obj3 = null;
		}
		else
		{
			CBaseEntity value3 = attacker.Value;
			if (value3 == null)
			{
				obj3 = null;
			}
			else
			{
				CCSPlayerPawn obj4 = ((NativeObject)value3).As<CCSPlayerPawn>();
				if (obj4 == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
					if (controller2 == null)
					{
						obj3 = null;
					}
					else
					{
						CBasePlayerController value4 = controller2.Value;
						obj3 = ((value4 != null) ? ((NativeObject)value4).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj3;
		if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid && _players.Contains(val2) && _voidEndTime.TryGetValue(val2, out var value5) && num < value5)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		if (_voidEndTime.TryGetValue(userid, out var value) && num < value)
		{
			return (HookResult)4;
		}
		return (HookResult)0;
	}
}
