using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace RollTheDice.Utils;

public static class MoveLockManager
{
	private static readonly Dictionary<ulong, HashSet<string>> _locks = new Dictionary<ulong, HashSet<string>>();

	public static void Lock(CCSPlayerController player, string key)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			ulong steamID = ((CBasePlayerController)player).SteamID;
			if (!_locks.ContainsKey(steamID))
			{
				_locks[steamID] = new HashSet<string>();
			}
			_locks[steamID].Add(key);
			ApplyLock(player);
		}
	}

	public static void Unlock(CCSPlayerController player, string key)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			_locks.Remove((player != null) ? ((CBasePlayerController)player).SteamID : 0);
			return;
		}
		ulong steamID = ((CBasePlayerController)player).SteamID;
		if (_locks.TryGetValue(steamID, out HashSet<string> value))
		{
			value.Remove(key);
			if (value.Count == 0)
			{
				_locks.Remove(steamID);
				RestoreMovement(player);
			}
		}
	}

	public static bool IsLocked(CCSPlayerController player)
	{
		HashSet<string> value;
		return _locks.TryGetValue(((CBasePlayerController)player).SteamID, out value) && value.Count > 0;
	}

	private static void ApplyLock(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseEntity)value).MoveType = (MoveType_t)0;
			Schema.SetSchemaValue<int>(((NativeEntity)value).Handle, "CBaseEntity", "m_nActualMoveType", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_MoveType", 0);
		}
	}

	private static void RestoreMovement(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseEntity)value).MoveType = (MoveType_t)2;
			Schema.SetSchemaValue<int>(((NativeEntity)value).Handle, "CBaseEntity", "m_nActualMoveType", 2);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_MoveType", 0);
		}
	}

	public static void Clear(ulong steamID)
	{
		_locks.Remove(steamID);
	}

	public static void ClearAll()
	{
		_locks.Clear();
	}
}
