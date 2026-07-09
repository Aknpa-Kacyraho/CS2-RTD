using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Cupid : DiceBlueprint
    {
        public override string ClassName => "Cupid";
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private bool _processingDeath;

        public Cupid(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (_processingDeath) return HookResult.Continue;

            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            _processingDeath = true;

            float delay = _config.Dices.Cupid.DeathDelay;

            // Capture the dice owner for kill credit
            CCSPlayerController diceOwner = victim;

            Server.NextFrame(() =>
            {
                // Pick a random alive player (any team) who is NOT the victim
                List<CCSPlayerController> alivePlayers = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p != diceOwner
                        && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                        && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .ToList();

                if (alivePlayers.Count == 0) return;

                CCSPlayerController target = alivePlayers[_random.Next(alivePlayers.Count)];

                new CounterStrikeSharp.API.Modules.Timers.Timer(delay, () =>
                {
                    if (target == null || !target.IsValid
                        || target.PlayerPawn?.Value == null || !target.PlayerPawn.Value.IsValid
                        || target.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        _processingDeath = false;
                        return;
                    }

                    if (!target.IsBot)
                        target.PlayerPawn.Value.CommitSuicide(false, true);
                    target.PrintToCenterAlert("💘 丘比特之箭射中了你!");
                    _processingDeath = false;
                });
            });

            return HookResult.Continue;
        }
    }
}
