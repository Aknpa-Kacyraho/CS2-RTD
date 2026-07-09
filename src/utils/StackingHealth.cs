using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils
{
    /// <summary>
    /// Shared registry for MaxHealth multipliers. Prevents snapshot corruption
    /// when multiple dice (Giant, Pope, Mosquito, etc.) modify the same player's
    /// MaxHealth independently.
    ///
    /// Usage:
    ///   Add:    StackingHealth.RegisterMultiplier(player, "Giant", 4f);
    ///           pawn.Health = pawn.MaxHealth;  // optional full heal
    ///   Remove: StackingHealth.UnregisterMultiplier(player, "Giant");
    ///   Reset:  StackingHealth.ClearAll();  // in OnRoundStart
    /// </summary>
    public static class StackingHealth
    {
        /// <summary>SteamID → { sourceKey → multiplier }</summary>
        private static readonly Dictionary<ulong, Dictionary<string, float>> _modifiers = [];

        /// <summary>CS2 default base HP per player SteamID (snapped on first use)</summary>
        private static readonly Dictionary<ulong, int> _baseHealth = [];

        public static void RegisterMultiplier(CCSPlayerController player, string key, float multiplier)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            ulong sid = player.SteamID;
            if (!_modifiers.ContainsKey(sid))
                _modifiers[sid] = [];

            // Snapshot base HP on first modifier for this player
            if (!_baseHealth.ContainsKey(sid))
                _baseHealth[sid] = 100; // CS2 default

            if (!_modifiers[sid].ContainsKey(key))
            {
                _modifiers[sid][key] = multiplier;
                Recompute(player);
            }
        }

        public static void UnregisterMultiplier(CCSPlayerController player, string key)
        {
            if (player == null || !player.IsValid)
            {
                _modifiers.Remove(player?.SteamID ?? 0);
                _baseHealth.Remove(player?.SteamID ?? 0);
                return;
            }

            ulong sid = player.SteamID;
            if (_modifiers.TryGetValue(sid, out var dict))
            {
                dict.Remove(key);
                if (dict.Count == 0)
                {
                    _modifiers.Remove(sid);
                    _baseHealth.Remove(sid);
                    // Restore to default
                    if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                    {
                        CCSPlayerPawn pawn = player.PlayerPawn.Value;
                        pawn.MaxHealth = 100;
                        if (pawn.Health > 100) pawn.Health = 100;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                }
                else
                {
                    Recompute(player);
                }
            }
        }

        /// <summary>Recompute effective MaxHealth from all active multipliers.</summary>
        public static void Recompute(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            ulong sid = player.SteamID;

            float product = 1.0f;
            if (_modifiers.TryGetValue(sid, out var dict))
            {
                foreach (float mult in dict.Values)
                    product *= mult;
            }

            int baseHp = _baseHealth.TryGetValue(sid, out int bh) ? bh : 100;
            int newMax = (int)float.Round(baseHp * product);

            pawn.MaxHealth = newMax;
            if (pawn.Health > newMax)
                pawn.Health = newMax;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        /// <summary>Clear per-player state (call on disconnect or round reset).</summary>
        public static void Clear(ulong steamID)
        {
            _modifiers.Remove(steamID);
            _baseHealth.Remove(steamID);
        }

        /// <summary>Clear ALL state (call in OnRoundStart).</summary>
        public static void ClearAll()
        {
            _modifiers.Clear();
            _baseHealth.Clear();
        }
    }
}
