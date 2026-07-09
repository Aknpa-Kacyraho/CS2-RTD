using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace RollTheDice.Utils
{
    /// <summary>
    /// Shared registry for MoveType locks. Prevents multiple dice (Titanfall,
    /// Gargoyle, IceBeam, Showdown) from fighting over MoveType — if any source
    /// still holds a lock, movement stays blocked.
    ///
    /// Usage:
    ///   Lock:   MoveLockManager.Lock(player, "Titanfall");
    ///   Unlock: MoveLockManager.Unlock(player, "Titanfall");
    ///   Reset:  MoveLockManager.ClearAll();  // in OnRoundStart
    /// </summary>
    public static class MoveLockManager
    {
        /// <summary>SteamID → set of lock source keys</summary>
        private static readonly Dictionary<ulong, HashSet<string>> _locks = [];

        public static void Lock(CCSPlayerController player, string key)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            ulong sid = player.SteamID;
            if (!_locks.ContainsKey(sid))
                _locks[sid] = [];

            _locks[sid].Add(key);
            ApplyLock(player);
        }

        public static void Unlock(CCSPlayerController player, string key)
        {
            if (player == null || !player.IsValid)
            {
                _locks.Remove(player?.SteamID ?? 0);
                return;
            }

            ulong sid = player.SteamID;
            if (_locks.TryGetValue(sid, out var set))
            {
                set.Remove(key);
                if (set.Count == 0)
                {
                    _locks.Remove(sid);
                    RestoreMovement(player);
                }
                // else: still locked by another source, keep MOVETYPE_NONE
            }
        }

        public static bool IsLocked(CCSPlayerController player)
        {
            return _locks.TryGetValue(player.SteamID, out var set) && set.Count > 0;
        }

        private static void ApplyLock(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.MoveType = MoveType_t.MOVETYPE_NONE;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 0);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }

        private static void RestoreMovement(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
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
}
