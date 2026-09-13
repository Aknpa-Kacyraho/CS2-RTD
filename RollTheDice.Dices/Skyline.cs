using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Skyline : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _flightEndTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Skyline";

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
			span[num2] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		float value;
		return _cooldowns.TryGetValue(player, out value) ? Math.Max(0f, value - Server.CurrentTime) : 0f;
	}

	public Skyline(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_cooldowns[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("☁ 天际就绪！按E飞行3秒！(冷却30秒)");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		EndFlight(player);
		_players.Remove(player);
		_cooldowns.Remove(player);
		_flightEndTime.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			EndFlight(item);
		}
		_players.Clear();
		_cooldowns.Clear();
		_flightEndTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void EndFlight(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseEntity)value).MoveType = (MoveType_t)2;
			Schema.SetSchemaValue<int>(((NativeEntity)value).Handle, "CBaseEntity", "m_nActualMoveType", 2);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_MoveType", 0);
			if (DiceSynergy.HasPartner(player, "NoRecoil") && RollTheDice.Instance?.HasDiceActive(player, "NoRecoil") != true)
			{
				player.ReplicateConVar("weapon_accuracy_nospread", "0");
			}
		}
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count != 0 && !((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _players.Contains(player) && ((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && ((CBaseEntity)player.PlayerPawn.Value).LifeState == 0)
		{
			float num = Server.CurrentTime;
			if (_cooldowns.TryGetValue(player, out var value) && num < value)
			{
				player.PrintToCenterAlert($"☁ 冷却中... {value - num:F0}秒");
				return;
			}
			float flightDuration = _config.Dices.Skyline.FlightDuration;
			CCSPlayerPawn value2 = player.PlayerPawn.Value;
			((CBaseEntity)value2).MoveType = (MoveType_t)7;
			Schema.SetSchemaValue<int>(((NativeEntity)value2).Handle, "CBaseEntity", "m_nActualMoveType", 7);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_MoveType", 0);
			_flightEndTime[player] = num + flightDuration;
			_cooldowns[player] = num + _config.Dices.Skyline.Cooldown;
			player.PrintToCenterAlert($"☁ 飞行中！{flightDuration}秒");
			if (DiceSynergy.HasPartner(player, "NoRecoil"))
			{
				NoRecoil.ApplyNoRecoil(player);
				player.PrintToCenterAlert("✈ 制空权！飞行期间零后坐零扩散！");
			}
		}
	}

	public void OnTick()
	{
		if (_flightEndTime.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<CCSPlayerController, float> item in _flightEndTime.ToList())
		{
			CCSPlayerController key = item.Key;
			if (num >= item.Value)
			{
				EndFlight(key);
				_flightEndTime.Remove(key);
				if (key != null)
				{
					key.PrintToCenterAlert("☁ 飞行结束!");
				}
			}
			else if ((CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)key.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value = key.PlayerPawn.Value;
				if ((int)((CBaseEntity)value).MoveType != 7)
				{
					((CBaseEntity)value).MoveType = (MoveType_t)7;
					Schema.SetSchemaValue<int>(((NativeEntity)value).Handle, "CBaseEntity", "m_nActualMoveType", 7);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_MoveType", 0);
				}
				if (DiceSynergy.HasPartner(key, "NoRecoil"))
				{
					NoRecoil.ApplyNoRecoil(key);
				}
			}
		}
	}
}
