using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class InfoHole : DiceBlueprint
    {
        public override string ClassName => "InfoHole";
        public override List<string> Listeners => ["OnTick"];

        public InfoHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });

            // Disable radar for enemies, enable for teammates
            ApplyRadarState();
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (_players.Count == 0)
            {
                // Restore radar for all players
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p == null || !p.IsValid || p.IsHLTV) continue;
                    p.ReplicateConVar("sv_disable_radar", "0");
                }
            }
        }

        public override void Reset()
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p == null || !p.IsValid || p.IsHLTV) continue;
                p.ReplicateConVar("sv_disable_radar", "0");
            }
            _players.Clear();
        }

        public override void Destroy() => Reset();

        private void ApplyRadarState()
        {
            if (_players.Count == 0) return;
            CCSPlayerController? owner = _players[0];
            if (owner == null || !owner.IsValid) return;

            foreach (var p in Utilities.GetPlayers())
            {
                if (p == null || !p.IsValid || p.IsHLTV) continue;
                if (p.TeamNum != owner.TeamNum)
                    p.ReplicateConVar("sv_disable_radar", "1");
                else
                    p.ReplicateConVar("sv_disable_radar", "0");
            }
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            // Re-apply radar state periodically in case engine resets it
            if (Server.TickCount % 128 == 0)
                ApplyRadarState();
        }
    }
}
