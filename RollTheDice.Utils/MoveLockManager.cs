using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// Shared movement scope manager. Each player has one saved original MoveType and
/// a stack of named scopes. The last scope wins; releasing the last scope restores
/// the pre-lock move type instead of forcing Walk.
/// </summary>
public static class MoveLockManager
{
	private sealed class LockState
	{
		public MoveScopeStack Stack = new MoveScopeStack();
		public int OriginalMoveType;
		public int OriginalActualMoveType;
	}

	private static readonly Dictionary<ulong, LockState> _locks = new Dictionary<ulong, LockState>();

	public static int ActiveLockCount => _locks.Sum(pair => pair.Value.Stack.Count);

	public static int LockedPlayerCount => _locks.Count;

	public static void Lock(CCSPlayerController player, string key)
	{
		Lock(player, key, MoveType_t.MOVETYPE_NONE);
	}

	public static void Lock(CCSPlayerController player, string key, MoveType_t target)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}

		ulong steamId = player.SteamID;
		if (!_locks.TryGetValue(steamId, out LockState state))
		{
			state = new LockState
			{
				// Capture both sides of the MoveType pair. Restoring only Walk is
				// wrong when another plugin/dice left the player in noclip/observer.
				OriginalMoveType = (int)((CBaseEntity)pawn).MoveType,
				OriginalActualMoveType = (int)((CBaseEntity)pawn).ActualMoveType
			};
			_locks[steamId] = state;
		}
		state.Stack.Push(key, (int)target);
		int? current = state.Stack.CurrentTarget;
		if (current.HasValue)
		{
			ApplyTarget(pawn, (MoveType_t)current.Value);
		}
	}

	public static void Unlock(CCSPlayerController player, string key)
	{
		if (player == null)
		{
			return;
		}

		ulong steamId;
		try
		{
			steamId = player.SteamID;
		}
		catch
		{
			return;
		}
		if (!_locks.TryGetValue(steamId, out LockState state))
		{
			return;
		}
		if (!state.Stack.Remove(key, out int? current))
		{
			return;
		}

		if (current.HasValue)
		{
			CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
			if (pawn != null && pawn.IsValid)
			{
				ApplyTarget(pawn, (MoveType_t)current.Value);
			}
			return;
		}

		_locks.Remove(steamId);
		CCSPlayerPawn? restorePawn = player.PlayerPawn?.Value;
		if (restorePawn != null && restorePawn.IsValid)
		{
			RestoreMovement(restorePawn, state);
		}
	}

	public static bool IsLocked(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return false;
		}
		return _locks.TryGetValue(player.SteamID, out LockState state) && state.Stack.Count > 0;
	}

	/// <summary>Drop a player's scopes. Restores the captured move types when the controller is still valid.</summary>
	public static void Clear(ulong steamID)
	{
		if (!_locks.Remove(steamID, out LockState state))
		{
			return;
		}
		CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault(p =>
			p != null && p.IsValid && p.SteamID == steamID);
		if (player != null)
		{
			RestoreMovement(player, state);
		}
	}

	public static void ClearAll()
	{
		foreach (ulong steamId in _locks.Keys.ToList())
		{
			LockState state = _locks[steamId];
			CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault(p =>
				p != null && p.IsValid && p.SteamID == steamId);
			if (player != null)
			{
				RestoreMovement(player, state);
			}
		}
		_locks.Clear();
	}

	private static void ApplyTarget(CCSPlayerPawn pawn, MoveType_t target)
	{
		int current = (int)target;
		CBaseEntity entity = (CBaseEntity)pawn;
		if ((int)entity.MoveType == current && (int)entity.ActualMoveType == current)
		{
			return;
		}
		entity.MoveType = target;
		entity.ActualMoveType = target;
		Utilities.SetStateChanged(entity, "CBaseEntity", "m_MoveType", 0);
	}

	private static void RestoreMovement(CCSPlayerController player, LockState state)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		RestoreMovement(pawn, state);
	}

	private static void RestoreMovement(CCSPlayerPawn pawn, LockState state)
	{
		((CBaseEntity)pawn).MoveType = (MoveType_t)state.OriginalMoveType;
		((CBaseEntity)pawn).ActualMoveType = (MoveType_t)state.OriginalActualMoveType;
		Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_MoveType", 0);
	}
}
