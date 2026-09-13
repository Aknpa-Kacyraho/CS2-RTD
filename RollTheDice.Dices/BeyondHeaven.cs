using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 超越天堂 BeyondHeaven：按 E 暂停时间 9 秒（冻结其他玩家移动），每回合仅一次。
/// 状态一律用 SteamID 记录，避免控制器对象变化导致判定失效。
/// </summary>
public class BeyondHeaven : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _timeStopEnd = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();

	private readonly HashSet<ulong> _used = new HashSet<ulong>();

	private readonly HashSet<ulong> _holders = new HashSet<ulong>();

	private bool _timeStopped;

	public override string ClassName => "BeyondHeaven";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public BeyondHeaven(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
		{
			return;
		}
		_players.Add(player);
		_holders.Add(player.SteamID);
		_used.Remove(player.SteamID);
		_cooldownEnd[player.SteamID] = 0f;
		RollTheDice.LogDebug($"[BeyondHeaven] Add: steamid={player.SteamID} holders={_holders.Count}\n");
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			player.PlayerName
		} });
		player.PrintToCenterAlert("🌌 按E键暂停时间9秒！仅可使用一次！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player != null)
		{
			_holders.Remove(player.SteamID);
			_timeStopEnd.Remove(player.SteamID);
			_cooldownEnd.Remove(player.SteamID);
			_used.Remove(player.SteamID);
		}
		_players.Remove(player);
		EndTimeStop();
	}

	public override void Reset()
	{
		EndTimeStop();
		_players.Clear();
		_holders.Clear();
		_timeStopEnd.Clear();
		_cooldownEnd.Clear();
		_used.Clear();
		_timeStopped = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (player == null || !player.IsValid || (pressed & PlayerButtons.Use) == 0)
		{
			return;
		}
		RollTheDice.LogDebug($"[BeyondHeaven] E press: steamid={player.SteamID} holders={_holders.Count} isHolder={_holders.Contains(player.SteamID)} used={_used.Contains(player.SteamID)} stopped={_timeStopped}\n");
		if (!_holders.Contains(player.SteamID) && !_players.Contains(player))
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || pawn.LifeState != 0)
		{
			return;
		}
		if (_used.Contains(player.SteamID))
		{
			return;
		}
		float now = Server.CurrentTime;
		if (_cooldownEnd.TryGetValue(player.SteamID, out var cooldown) && now < cooldown)
		{
			return;
		}
		_timeStopEnd[player.SteamID] = now + _config.Dices.BeyondHeaven.Duration;
		_cooldownEnd[player.SteamID] = now + _config.Dices.BeyondHeaven.Cooldown;
		_used.Add(player.SteamID);
		_timeStopped = true;
		LockOthers(player);
		RollTheDice.LogDebug($"[BeyondHeaven] activated (steamid={player.SteamID})\n");
		player.PrintToCenterAlert("🌌 超越天堂！时间暂停9s！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌌 {player.PlayerName} 超越了天堂！时间暂停9秒！");
	}

	public void OnTick()
	{
		if (!_timeStopped)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (KeyValuePair<ulong, float> kv in _timeStopEnd.ToList())
		{
			if (now >= kv.Value)
			{
				_timeStopEnd.Remove(kv.Key);
				EndTimeStop();
				CCSPlayerController expiring = FindBySteamId(kv.Key);
				expiring?.PrintToCenterAlert("⏰ 时间恢复流动！");
				break;
			}
			foreach (CCSPlayerController other in Utilities.GetPlayers())
			{
				if (other == null || !other.IsValid || other.IsHLTV || other.SteamID == kv.Key)
				{
					continue;
				}
				CCSPlayerPawn otherPawn = other.PlayerPawn?.Value;
				if (otherPawn == null || !otherPawn.IsValid || otherPawn.LifeState != 0)
				{
					continue;
				}
				MoveLockManager.Lock(other, "BeyondHeaven");
			}
			CCSPlayerController holder = FindBySteamId(kv.Key);
			if (holder != null)
			{
				holder.PrintToCenterAlert($"🌌 超越天堂！{(int)Math.Ceiling(kv.Value - now)}s 剩余");
			}
		}
	}

	private void LockOthers(CCSPlayerController holder)
	{
		foreach (CCSPlayerController other in Utilities.GetPlayers())
		{
			if (other == null || !other.IsValid || other.IsHLTV || other == holder)
			{
				continue;
			}
			CCSPlayerPawn otherPawn = other.PlayerPawn?.Value;
			if (otherPawn == null || !otherPawn.IsValid || otherPawn.LifeState != 0)
			{
				continue;
			}
			MoveLockManager.Lock(other, "BeyondHeaven");
		}
	}

	private void EndTimeStop()
	{
		if (!_timeStopped)
		{
			return;
		}
		_timeStopped = false;
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			if (player != null && player.IsValid)
			{
				MoveLockManager.Unlock(player, "BeyondHeaven");
			}
		}
	}

	private static CCSPlayerController? FindBySteamId(ulong steamId)
	{
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && p.SteamID == steamId);
	}
}
