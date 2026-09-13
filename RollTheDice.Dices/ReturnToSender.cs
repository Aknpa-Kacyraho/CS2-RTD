using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 遣返 ReturnToSender：命中累积，每第 N 次命中把敌人送回出生点（可预期，非随机）。
/// </summary>
public class ReturnToSender : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<ulong, int> _hits = new Dictionary<ulong, int>();

	private CBaseEntity[] playerSpawnEntities = Array.Empty<CBaseEntity>();

	private CBaseEntity[] ctSpawnEntities = Array.Empty<CBaseEntity>();

	private CBaseEntity[] tSpawnEntities = Array.Empty<CBaseEntity>();

	public override string ClassName => "ReturnToSender";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public ReturnToSender(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_hits[player.SteamID] = 0;
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_hits.Remove(player.SteamID);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_hits.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		ulong attackerId = attacker.SteamID;
		int hits = (_hits.TryGetValue(attackerId, out int h) ? h : 0) + 1;
		int required = Math.Max(1, _config.Dices.ReturnToSender.HitsRequired);
		if (hits < required)
		{
			_hits[attackerId] = hits;
			return HookResult.Continue;
		}
		_hits[attackerId] = 0;
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (victimPawn == null || !victimPawn.IsValid || ((CBaseEntity)victimPawn).LifeState != 0)
		{
			return HookResult.Continue;
		}
		GetSpawnEntities();
		CBaseEntity[] teamSpawns = ((int)victim.Team == 3) ? ctSpawnEntities : tSpawnEntities;
		CBaseEntity spawn = teamSpawns.Concat(playerSpawnEntities).OrderBy((CBaseEntity _) => _random.Next()).FirstOrDefault((CBaseEntity s) => s != null && s.IsValid && !IsPlayerNearby(s.AbsOrigin));
		if (spawn == null)
		{
			return HookResult.Continue;
		}
		((CBaseEntity)victimPawn).Teleport(spawn.AbsOrigin, null, null);
		NotifyStatus(victim, ClassName, new Dictionary<string, string>());
		attacker.PrintToCenterAlert($"↩ 第 {required} 次命中，将 {((CBasePlayerController)victim).PlayerName} 遣返出生点！");
		return HookResult.Continue;
	}

	private void GetSpawnEntities()
	{
		if (playerSpawnEntities.Length == 0 && ctSpawnEntities.Length == 0 && tSpawnEntities.Length == 0)
		{
			playerSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_start").ToArray();
			ctSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist").ToArray();
			tSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist").ToArray();
		}
	}

	private bool IsPlayerNearby(Vector position)
	{
		if (position == null)
		{
			return false;
		}
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			Vector origin = ((CBaseEntity)pawn).AbsOrigin;
			if (origin != null && Vectors.GetDistance(position, origin) < 100f)
			{
				return true;
			}
		}
		return false;
	}
}
