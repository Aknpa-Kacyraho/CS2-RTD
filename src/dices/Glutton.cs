using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Glutton : DiceBlueprint
    {
        public override string ClassName => "Glutton";
        public override List<string> Events => ["EventPlayerDeath"];
        public static readonly Dictionary<ulong, int> KillCounts = [];

        public Glutton(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker) || attacker == @event.Userid)
                return HookResult.Continue;

            int maxExtra = _config.Dices.Glutton.MaxExtraDice;
            int current = KillCounts.TryGetValue(attacker.SteamID, out int kills) ? kills : 0;
            if (current < maxExtra)
                KillCounts[attacker.SteamID] = current + 1;

            return HookResult.Continue;
        }
    }
}
