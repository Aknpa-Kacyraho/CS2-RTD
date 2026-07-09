using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class DivinePunishment : DiceBlueprint
    {
        public override string ClassName => "DivinePunishment";
        public override List<string> Events => ["EventPlayerDeath"];
        private readonly Dictionary<CCSPlayerController, int> _killCounts = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public DivinePunishment(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _killCounts[player] = 0;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _killCounts.Remove(player);
        }

        public override void Reset() { _players.Clear(); _killCounts.Clear(); }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;
            if (attacker == @event.Userid) return HookResult.Continue;

            int current = _killCounts.TryGetValue(attacker, out int k) ? k : 0;
            current++;
            _killCounts[attacker] = current;

            int required = _config.Dices.DivinePunishment.KillsRequired;
            if (current < required)
            {
                // Progress message
                attacker.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_DivinePunishment_progress"].Value.Replace("{current}", current.ToString()).Replace("{required}", required.ToString())}");
                return HookResult.Continue;
            }

            // Reset counter
            _killCounts[attacker] = 0;

            // Find random alive enemy (NOT on dice holder's team)
            var enemies = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum != attacker.TeamNum
                    && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                    && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();
            if (enemies.Count == 0) return HookResult.Continue;

            var target = enemies[_random.Next(enemies.Count)];
            CCSPlayerController capturedTarget = target;
            string attackerName = attacker.PlayerName;

            // Use Server.NextFrame to avoid executing death inside death event
            Server.NextFrame(() =>
            {
                if (capturedTarget == null || !capturedTarget.IsValid
                    || capturedTarget.PlayerPawn?.Value == null || !capturedTarget.PlayerPawn.Value.IsValid
                    || capturedTarget.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    return;

                if (!capturedTarget.IsBot && !capturedTarget.IsHLTV)
                    capturedTarget.PlayerPawn.Value.CommitSuicide(false, true);
                else
                {
                    try { capturedTarget.PlayerPawn.Value.CommitSuicide(false, true); }
                    catch
                    {
                        capturedTarget.PlayerPawn.Value.Health = 0;
                        Utilities.SetStateChanged(capturedTarget.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                    }
                }
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_DivinePunishment_trigger"].Value.Replace("{attacker}", attackerName).Replace("{target}", capturedTarget.PlayerName)}");
            });

            return HookResult.Continue;
        }
    }
}
