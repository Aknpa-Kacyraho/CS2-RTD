using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Mimic : DiceBlueprint
    {
        public override string ClassName => "Mimic";
        public override List<string> Events => ["EventPlayerDeath"];

        /// <summary>
        /// Static dictionary for core system.
        /// Key = attacker SteamID, Value = dice class name to grant.
        /// Empty string means "pending — core OnPlayerDeath will fill in the class name".
        /// Non-empty means "ready to be granted in the extra-dice loop".
        /// </summary>
        public static Dictionary<ulong, string> PendingCopy = [];

        public Mimic(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        public override void Reset()
        {
            _players.Clear();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (attacker == @event.Userid) return HookResult.Continue;

            // Mark this attacker for mimic copy — core will fill in the dice class name
            PendingCopy[attacker.SteamID] = "";
            return HookResult.Continue;
        }
    }
}
