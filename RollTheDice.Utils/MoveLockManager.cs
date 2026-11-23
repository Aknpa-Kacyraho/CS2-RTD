using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace RollTheDice.Utils;

public static class MoveLockManager
{
	private sealed class LockState
	{
		public HashSet<string> Keys = new HashSet<string>();
		public int OriginalMoveType;
	}

	private static readonly Dictionary<ulong, LockState> _locks = new Dictionary<ulong, LockState>();

	public static void Lock(CCSPlayerController player, string key)
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
				// Capture the real pre-lock state. Unlock must not assume Walk:
				// another plugin/dice may have put the player in a special MoveType.
				OriginalMoveType = (int)((CBaseEntity)pawn).MoveType
			};
			_locks[steamId] = state;
		}
		state.Keys.Add(key);
		ApplyLock(player);
	}

	public static void Unlock(CCSPlayerController player, string key)
	{
		if (player == null)
		{
			return;
		}

		ulong steamId = player.SteamID;
		if (!_locks.TryGetValue(steamId, out LockState state))
		{
			return;
		}

		state.Keys.Remove(key);
		if (state.Keys.Count == 0)
		{
			_locks.Remove(steamId);
			RestoreMovement(player, state);
		}
	}

	public static bool IsLocked(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return false;
		}
		return _locks.TryGetValue(player.SteamID, out LockState state) && state.Keys.Count > 0;
	}

	private static void ApplyLock(CCSPlayerController player)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		((CBaseEntity)pawn).MoveType = (MoveType_t)0;
		Schema.SetSchemaValue<int>(((NativeEntity)pawn).Handle, "CBaseEntity", "m_nActualMoveType", 0);
		Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_MoveType", 0);
	}

	private static void RestoreMovement(CCSPlayerController player, LockState state)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		((CBaseEntity)pawn).MoveType = (MoveType_t)state.OriginalMoveType;
		Schema.SetSchemaValue<int>(((NativeEntity)pawn).Handle, "CBaseEntity", "m_nActualMoveType", state.OriginalMoveType);
		Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_MoveType", 0);
	}

	/// <summary>Drop a player's locks. Restores the captured move type when the controller is still valid.</summary>
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
}
