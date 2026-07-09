using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Linq;

namespace RollTheDice.Dices
{
    public class Pope : DiceBlueprint
    {
        public override string ClassName => "Pope";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick"];

        // Track buffed teammates and their original MaxHealth values
        // Pope → (teammate SteamID → original MaxHealth before buff)
        private readonly Dictionary<CCSPlayerController, Dictionary<ulong, int>> _buffedTeammates = [];

        public Pope(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Priest");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "神圣共鸣", "神圣共鸣联动生效！");
            _buffedTeammates[player] = [];
            ApplyBuffToTeammates(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            RestoreTeammates(player);
            _ = _players.Remove(player);
            _ = _buffedTeammates.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                RestoreTeammates(player);
            }
            _players.Clear();
            _buffedTeammates.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            // Maintain — check for newly spawned/respawned teammates each tick
            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    ApplyBuffToTeammates(player);
                }
                catch { }
            }
        }

        private void ApplyBuffToTeammates(CCSPlayerController pope)
        {
            if (!_buffedTeammates.TryGetValue(pope, out var buffed)) return;

            int multiplier = _config.Dices.Pope.HealthMultiplier + (_comboActive ? 1 : 0);

            foreach (var teammate in Utilities.GetPlayers()
                .Where(p => p.IsValid && p.PlayerPawn?.Value?.IsValid == true
                    && p.TeamNum == pope.TeamNum
                    && p != pope))
            {
                ulong sid = teammate.SteamID;

                // Skip if already buffed by this Pope instance
                if (buffed.ContainsKey(sid)) continue;

                CCSPlayerPawn tPawn = teammate.PlayerPawn.Value;

                // Save original MaxHealth (could be 100 default, 444 from RoyalBarrier, etc.)
                int originalMax = tPawn.MaxHealth;
                buffed[sid] = originalMax;

                // Apply Pope multiplier on top of whatever MaxHealth the teammate currently has
                int newMax = originalMax * multiplier;
                tPawn.MaxHealth = newMax;
                tPawn.Health = newMax;
                Utilities.SetStateChanged(tPawn, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(tPawn, "CBaseEntity", "m_iHealth");
            }
        }

        private void RestoreTeammates(CCSPlayerController pope)
        {
            if (!_buffedTeammates.TryGetValue(pope, out var buffed)) return;

            foreach (var kvp in buffed)
            {
                ulong sid = kvp.Key;
                int originalMax = kvp.Value;

                var teammate = Utilities.GetPlayers()
                    .FirstOrDefault(p => p.IsValid && p.SteamID == sid);
                if (teammate?.PlayerPawn?.Value != null && teammate.PlayerPawn.Value.IsValid)
                {
                    CCSPlayerPawn tPawn = teammate.PlayerPawn.Value;
                    tPawn.MaxHealth = originalMax;
                    if (tPawn.Health > originalMax)
                        tPawn.Health = originalMax;
                    Utilities.SetStateChanged(tPawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(tPawn, "CBaseEntity", "m_iHealth");
                }
            }
            buffed.Clear();
        }
    }
}
