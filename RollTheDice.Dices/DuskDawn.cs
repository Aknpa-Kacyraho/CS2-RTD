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

public class DuskDawn : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, bool> _triggered = new Dictionary<CCSPlayerController, bool>();

	private readonly Dictionary<CCSPlayerController, float> _lastHealTime = new Dictionary<CCSPlayerController, float>();

	private static readonly HashSet<ulong> _forceTriggered = new HashSet<ulong>();

	public override string ClassName => "DuskDawn";

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

	public static void TriggerDawn(CCSPlayerController player)
	{
		_forceTriggered.Add(((CBasePlayerController)player).SteamID);
	}

	public DuskDawn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			bool flag = _forceTriggered.Contains(((CBasePlayerController)player).SteamID);
			_triggered[player] = flag;
			_lastHealTime[player] = 0f;
			_comboActive = DiceSynergy.HasPartner(player, "Cthulhu");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "深渊觉醒", "克苏恩+暮光！深渊觉醒了！");
			}
			if (flag)
			{
				CCSPlayerPawn value = player.PlayerPawn.Value;
				((CBaseEntity)value).MaxHealth = _config.Dices.DuskDawn.MaxHealth;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, ((CBaseEntity)value).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				player.PrintToCenterAlert("☀\ufe0f 深渊觉醒！破晓已触发！");
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☀\ufe0f {((CBasePlayerController)player).PlayerName} 深渊觉醒！破晓降临！");
			}
			else
			{
				NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				} });
				player.PrintToCenterAlert("\ud83c\udf05 暮光：命悬一线时将触发破晓！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_triggered.Remove(player);
		_lastHealTime.Remove(player);
		_forceTriggered.Remove(((CBasePlayerController)player).SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_triggered.Clear();
		_lastHealTime.Clear();
		_forceTriggered.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		if (info.Damage <= 0f)
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
		CCSPlayerController val = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		if (_triggered.TryGetValue(val, out var value2) & value2)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn val2 = val.PlayerPawn?.Value;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid)
		{
			return (HookResult)0;
		}
		int num = ((CBaseEntity)val2).Health - (int)float.Round(info.Damage);
		if (num > 0)
		{
			return (HookResult)0;
		}
		_triggered[val] = true;
		info.Damage = Math.Max(0, ((CBaseEntity)val2).Health - 1);
		((CBaseEntity)val2).MaxHealth = _config.Dices.DuskDawn.MaxHealth;
		Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iMaxHealth", 0);
		val.PrintToCenterAlert("☀\ufe0f 破晓！每秒回复100HP+10护甲，上限150HP！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☀\ufe0f {((CBasePlayerController)val).PlayerName} 触发破晓！每秒回复100HP+10护甲！");
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
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || !_triggered.TryGetValue(item, out var value) || !value || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			CCSPlayerPawn value2 = item.PlayerPawn.Value;
			if (!_lastHealTime.TryGetValue(item, out var value3) || num - value3 >= 1f)
			{
				_lastHealTime[item] = num;
				int maxHealth = _config.Dices.DuskDawn.MaxHealth;
				int healPerSec = _config.Dices.DuskDawn.HealPerSec;
				int armorPerSec = _config.Dices.DuskDawn.ArmorPerSec;
				if (((CBaseEntity)value2).Health < maxHealth)
				{
					((CBaseEntity)value2).Health = Math.Min(((CBaseEntity)value2).Health + healPerSec, maxHealth);
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				}
				if (value2.ArmorValue < 100)
				{
					value2.ArmorValue = Math.Min(value2.ArmorValue + armorPerSec, 100);
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_ArmorValue", 0);
				}
				item.PrintToCenterAlert($"☀\ufe0f 破晓 {((CBaseEntity)value2).Health}/{maxHealth}HP +{armorPerSec}甲/s");
			}
		}
	}
}
