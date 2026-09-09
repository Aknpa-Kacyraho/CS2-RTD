using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class BeyondHeaven : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _timeStopEnd = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _cooldownEnd = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, bool> _used = new Dictionary<CCSPlayerController, bool>();

	private bool _timeStopped;

	public override string ClassName => "BeyondHeaven";

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
			span[num2] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public BeyondHeaven(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_used[player] = false;
			_cooldownEnd[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83c\udf0c 按E键暂停时间9秒！仅可使用一次！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		EndTimeStop();
		_players.Remove(player);
		_timeStopEnd.Remove(player);
		_cooldownEnd.Remove(player);
		_used.Remove(player);
	}

	public override void Reset()
	{
		EndTimeStop();
		_players.Clear();
		_timeStopEnd.Clear();
		_cooldownEnd.Clear();
		_used.Clear();
		_timeStopped = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	private void EndTimeStop()
	{
		if (!_timeStopped)
		{
			return;
		}
		_timeStopped = false;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			MoveLockManager.Unlock(item, "BeyondHeaven");
		}
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if ((_cooldownEnd.TryGetValue(player, out var value) && num < value) || (_used.TryGetValue(player, out var value2) & value2))
		{
			return;
		}
		_timeStopEnd[player] = num + _config.Dices.BeyondHeaven.Duration;
		_cooldownEnd[player] = num + _config.Dices.BeyondHeaven.Cooldown;
		_used[player] = true;
		_timeStopped = true;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)player && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			MoveLockManager.Lock(item, "BeyondHeaven");
		}
		player.PrintToCenterAlert("\ud83c\udf0c 超越天堂！时间暂停9s！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udf0c {((CBasePlayerController)player).PlayerName} 超越了天堂！时间暂停9秒！");
	}

	public void OnTick()
	{
		if (!_timeStopped)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<CCSPlayerController, float> kv in _timeStopEnd.ToList())
		{
			if (num >= kv.Value)
			{
				_timeStopEnd.Remove(kv.Key);
				EndTimeStop();
				CCSPlayerController key = kv.Key;
				if (key != null)
				{
					key.PrintToCenterAlert("⏰ 时间恢复流动！");
				}
				break;
			}
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)kv.Key && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p)
			{
				MoveLockManager.Lock(item, "BeyondHeaven");
			}
			int value = (int)Math.Ceiling(kv.Value - num);
			CCSPlayerController key2 = kv.Key;
			if (key2 != null)
			{
				key2.PrintToCenterAlert($"\ud83c\udf0c 超越天堂！{value}s 剩余");
			}
		}
	}
}
