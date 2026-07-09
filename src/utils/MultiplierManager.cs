using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils
{
    public static class DamageBonusManager
    {
        private const float DefaultCap = 0.5f;
        private static readonly Dictionary<ulong, Dictionary<string, float>> _bonuses = [];

        public static void Register(CCSPlayerController player, string source, float percentage)
            => RegisterBySteamId(player.SteamID, source, percentage);

        public static void RegisterBySteamId(ulong steamId, string source, float percentage)
        {
            if (!_bonuses.ContainsKey(steamId))
                _bonuses[steamId] = [];
            _bonuses[steamId][source] = percentage;
        }

        public static void Unregister(CCSPlayerController player, string source)
            => UnregisterBySteamId(player.SteamID, source);

        public static void UnregisterBySteamId(ulong steamId, string source)
        {
            if (_bonuses.TryGetValue(steamId, out var bonuses))
            {
                bonuses.Remove(source);
                if (bonuses.Count == 0)
                    _bonuses.Remove(steamId);
            }
        }

        public static float GetEffective(CCSPlayerController player, float cap = DefaultCap)
            => GetEffectiveBySteamId(player.SteamID, cap);

        public static float GetEffectiveBySteamId(ulong steamId, float cap = DefaultCap)
        {
            if (_bonuses.TryGetValue(steamId, out var bonuses) && bonuses.Count > 0)
                return Math.Min(bonuses.Values.Max(), Math.Min(cap, DefaultCap));
            return 0f;
        }

        public static bool IsHighest(CCSPlayerController player, string source)
        {
            if (!_bonuses.TryGetValue(player.SteamID, out var bonuses) || bonuses.Count == 0)
                return false;
            if (!bonuses.TryGetValue(source, out float myBonus))
                return false;
            float max = bonuses.Values.Max();
            return MathF.Abs(myBonus - max) < 0.001f;
        }

        public static bool HasAny(CCSPlayerController player)
            => _bonuses.TryGetValue(player.SteamID, out var bonuses) && bonuses.Count > 0;

        public static void ClearAll() => _bonuses.Clear();
    }

    public static class SpeedBonusManager
    {
        private const float DefaultCap = 0.5f;
        private static readonly Dictionary<ulong, Dictionary<string, float>> _bonuses = [];

        public static void Register(CCSPlayerController player, string source, float percentage)
            => RegisterBySteamId(player.SteamID, source, percentage);

        public static void RegisterBySteamId(ulong steamId, string source, float percentage)
        {
            if (!_bonuses.ContainsKey(steamId))
                _bonuses[steamId] = [];
            _bonuses[steamId][source] = percentage;
        }

        public static void Unregister(CCSPlayerController player, string source)
            => UnregisterBySteamId(player.SteamID, source);

        public static void UnregisterBySteamId(ulong steamId, string source)
        {
            if (_bonuses.TryGetValue(steamId, out var bonuses))
            {
                bonuses.Remove(source);
                if (bonuses.Count == 0)
                    _bonuses.Remove(steamId);
            }
        }

        public static float GetEffective(CCSPlayerController player, float cap = DefaultCap)
            => GetEffectiveBySteamId(player.SteamID, cap);

        public static float GetEffectiveBySteamId(ulong steamId, float cap = DefaultCap)
        {
            if (_bonuses.TryGetValue(steamId, out var bonuses) && bonuses.Count > 0)
                return Math.Min(bonuses.Values.Max(), Math.Min(cap, DefaultCap));
            return 0f;
        }

        public static bool HasAny(CCSPlayerController player)
            => _bonuses.TryGetValue(player.SteamID, out var bonuses) && bonuses.Count > 0;

        public static void ClearAll() => _bonuses.Clear();
    }

    public static class DamageReductionManager
    {
        private const float DefaultCap = 0.5f;
        private static readonly Dictionary<ulong, Dictionary<string, float>> _reductions = [];

        public static void Register(CCSPlayerController player, string source, float percentage)
            => RegisterBySteamId(player.SteamID, source, percentage);

        public static void RegisterBySteamId(ulong steamId, string source, float percentage)
        {
            if (!_reductions.ContainsKey(steamId))
                _reductions[steamId] = [];
            _reductions[steamId][source] = percentage;
        }

        public static void Unregister(CCSPlayerController player, string source)
            => UnregisterBySteamId(player.SteamID, source);

        public static void UnregisterBySteamId(ulong steamId, string source)
        {
            if (_reductions.TryGetValue(steamId, out var reductions))
            {
                reductions.Remove(source);
                if (reductions.Count == 0)
                    _reductions.Remove(steamId);
            }
        }

        public static float GetEffective(CCSPlayerController player, float cap = DefaultCap)
            => GetEffectiveBySteamId(player.SteamID, cap);

        public static float GetEffectiveBySteamId(ulong steamId, float cap = DefaultCap)
        {
            if (_reductions.TryGetValue(steamId, out var reductions) && reductions.Count > 0)
                return Math.Min(reductions.Values.Max(), cap);
            return 0f;
        }

        public static bool HasAny(CCSPlayerController player)
            => _reductions.TryGetValue(player.SteamID, out var reductions) && reductions.Count > 0;

        public static void ClearAll() => _reductions.Clear();
    }
}
