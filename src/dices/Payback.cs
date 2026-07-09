using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Payback : DiceBlueprint
    {
        public override string ClassName => "Payback";
        public override List<string> Events => [
            "EventPlayerDeath"
        ];

        public Payback(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            CCSPlayerController? victim = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (victim == null || !victim.IsValid || !_players.Contains(victim)
                || attacker == null || !attacker.IsValid || attacker == victim
                || attacker.PlayerPawn?.Value == null || !attacker.PlayerPawn.Value.IsValid) return HookResult.Continue;

            CCSPlayerController capturedAttacker = attacker;
            string victimName = victim.PlayerName;

            // Use Server.NextFrame to avoid modifying HP inside death event
            Server.NextFrame(() =>
            {
                if (capturedAttacker == null || !capturedAttacker.IsValid
                    || capturedAttacker.PlayerPawn?.Value == null || !capturedAttacker.PlayerPawn.Value.IsValid)
                    return;

                CCSPlayerPawn killerPawn = capturedAttacker.PlayerPawn.Value;

                // Deal 50 HP damage
                killerPawn.Health -= 50;
                Utilities.SetStateChanged(killerPawn, "CBaseEntity", "m_iHealth");

                // Clear killer's money
                if (capturedAttacker.InGameMoneyServices != null)
                {
                    capturedAttacker.InGameMoneyServices.Account = 0;
                    Utilities.SetStateChanged(capturedAttacker, "CCSPlayerController", "m_pInGameMoneyServices");
                }

                if (killerPawn.Health <= 0 && killerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    if (!capturedAttacker.IsBot)
                        killerPawn.CommitSuicide(false, true);
                }

                capturedAttacker.PrintToCenterAlert($"☠ 以牙还牙! -50 HP + 金钱清零!");
            });

            return HookResult.Continue;
        }
    }
}
