using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Traitor : DiceBlueprint
    {
        public override string ClassName => "Traitor";
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Traitor(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            CCSPlayerController? victim = @event.Userid;

            // Only trigger when the ATTACKER has Traitor dice
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (victim == null || !victim.IsValid || attacker == victim) return HookResult.Continue;

            // Must be a teammate kill
            if (victim.TeamNum != attacker.TeamNum) return HookResult.Continue;

            float delay = _config.Dices.Traitor.DeathDelay;
            string attName = attacker.PlayerName;
            string vicName = victim.PlayerName;
            int attackerTeam = attacker.TeamNum;

            // Follow Cupid's proven pattern: NextFrame + Timer delay
            Server.NextFrame(() =>
            {
                // Pick a random alive enemy (different team from attacker)
                List<CCSPlayerController> enemies = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.TeamNum != attackerTeam
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .ToList();

                if (enemies.Count == 0) return;

                CCSPlayerController target = enemies[_random.Next(enemies.Count)];
                string enemyName = target.PlayerName;

                new CounterStrikeSharp.API.Modules.Timers.Timer(delay, () =>
                {
                    if (target == null || !target.IsValid
                        || target.PlayerPawn?.Value == null || !target.PlayerPawn.Value.IsValid
                        || target.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        return;

                    if (!target.IsBot && !target.IsHLTV)
                        target.PlayerPawn.Value.CommitSuicide(false, true);
                    else
                    {
                        try { target.PlayerPawn.Value.CommitSuicide(false, true); }
                        catch
                        {
                            target.PlayerPawn.Value.Health = 0;
                            Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    }
                    target.PrintToCenterAlert("🔪 叛徒出卖了你!");
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Traitor_broadcast"].Value.Replace("{attacker}", attName).Replace("{victim}", vicName).Replace("{enemy}", enemyName)}");
                });
            });

            return HookResult.Continue;
        }
    }
}
