using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 忒尼尔 Teneril：每有一名队友阵亡，随机诅咒一名敌人——减速 slow_percent 持续 slow_seconds，
/// 且其当前血量乘以 health_multiplier（下限 min_health）。
/// </summary>
public class Teneril : DiceBlueprint
{
	private readonly HashSet<ulong> _holderIds = new HashSet<ulong>();

	private readonly Dictionary<ulong, float> _slowUntil = new Dictionary<ulong, float>();

	private readonly Random _random = new Random();

	public override string ClassName => "Teneril";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public Teneril(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Add(CCSPlayerController player)
	{
		base.Add(player);
		if (_players.Contains(player))
		{
			_holderIds.Add(player.SteamID);
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		_players.Remove(player);
		_holderIds.Remove(player.SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_holderIds.Clear();
		_slowUntil.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if ((CEntityInstance)(object)holder == (CEntityInstance)null || !((CEntityInstance)holder).IsValid || (CEntityInstance)(object)holder == (CEntityInstance)(object)victim || ((CBaseEntity)holder).TeamNum != ((CBaseEntity)victim).TeamNum)
			{
				continue;
			}
			CCSPlayerController target = PickRandomEnemy(holder);
			if (target != null)
			{
				ApplyCurse(target);
			}
		}
		return (HookResult)0;
	}

	private CCSPlayerController? PickRandomEnemy(CCSPlayerController holder)
	{
		List<CCSPlayerController> enemies = Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			if (p == null || !((CEntityInstance)p).IsValid || ((CBasePlayerController)p).IsHLTV || (CEntityInstance)(object)p == (CEntityInstance)(object)holder)
			{
				return false;
			}
			if (((CBaseEntity)p).TeamNum == ((CBaseEntity)holder).TeamNum)
			{
				return false;
			}
			return (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0;
		}).ToList();
		if (enemies.Count == 0)
		{
			return null;
		}
		return enemies[_random.Next(enemies.Count)];
	}

	private void ApplyCurse(CCSPlayerController enemy)
	{
		CCSPlayerPawn pawn = enemy.PlayerPawn?.Value;
		if ((CEntityInstance)(object)pawn == (CEntityInstance)null || !((CEntityInstance)pawn).IsValid || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		int halved = Math.Max(_config.Dices.Teneril.MinHealth, (int)Math.Floor((float)((CBaseEntity)pawn).Health * _config.Dices.Teneril.HealthMultiplier));
		((CBaseEntity)pawn).Health = halved;
		Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iHealth", 0);
		ulong steamID = ((CBasePlayerController)enemy).SteamID;
		float seconds = _config.Dices.Teneril.SlowSeconds;
		_slowUntil[steamID] = Server.CurrentTime + seconds;
		SpeedBonusManager.Register(enemy, ClassName, -_config.Dices.Teneril.SlowPercent);
		pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(enemy, 100f);
		Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		enemy.PrintToCenterAlert($"忒尼尔的诅咒！减速{_config.Dices.Teneril.SlowPercent * 100f:F0}% {seconds:F0}s");
		new Timer(seconds, (Action)delegate
		{
			RestoreSlow(steamID);
		}, (TimerFlags?)null);
	}

	private void RestoreSlow(ulong steamID)
	{
		if (_slowUntil.TryGetValue(steamID, out float until) && Server.CurrentTime < until - 0.05f)
		{
			return;
		}
		_slowUntil.Remove(steamID);
		CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == steamID);
		if (player == null)
		{
			return;
		}
		SpeedBonusManager.Unregister(player, ClassName);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if ((CEntityInstance)(object)pawn != (CEntityInstance)null && ((CEntityInstance)pawn).IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}
}
