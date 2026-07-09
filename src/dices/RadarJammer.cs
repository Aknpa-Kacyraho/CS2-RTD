using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class RadarJammer : DiceBlueprint
    {
        public override string ClassName => "RadarJammer";
        public override List<string> Events => ["EventPlayerDeath"];
        private readonly Dictionary<CCSPlayerController, float> _cooldowns = [];
        private readonly Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer?> _restoreTimers = [];

        public RadarJammer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _cooldowns[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("📡 雷达干扰！击杀后黑掉敌人雷达20秒！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            RestoreRadarForPlayer(player);
            _ = _players.Remove(player);
            _ = _cooldowns.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                RestoreRadarForPlayer(p);
            _players.Clear();
            _cooldowns.Clear();
        }

        public override void Destroy() => Reset();

        private void RestoreRadarForPlayer(CCSPlayerController jammer)
        {
            if (_restoreTimers.TryGetValue(jammer, out var timer))
            {
                timer?.Kill();
                _ = _restoreTimers.Remove(jammer);
            }
            if (jammer == null || !jammer.IsValid) return;
            var enemies = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum != jammer.TeamNum);
            foreach (var enemy in enemies)
            {
                enemy.ReplicateConVar("sv_disable_radar", "0");
            }
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            if (_cooldowns.TryGetValue(attacker, out float cd) && now < cd) return HookResult.Continue;

            float blackoutDuration = _config.Dices.RadarJammer.BlackoutDuration;
            float cooldownDuration = _config.Dices.RadarJammer.Cooldown;

            _cooldowns[attacker] = now + cooldownDuration;

            var enemies = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum != attacker.TeamNum);
            foreach (var enemy in enemies)
            {
                enemy.ReplicateConVar("sv_disable_radar", "1");
                enemy.PrintToCenterAlert("📡 雷达被黑！20秒");
            }

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{attacker.PlayerName} 击杀了敌人，敌方雷达全部被黑！");

            CCSPlayerController captured = attacker;
            var restoreTimer = new CounterStrikeSharp.API.Modules.Timers.Timer(blackoutDuration, () =>
            {
                var allEnemies = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.TeamNum != captured.TeamNum);
                foreach (var enemy in allEnemies)
                {
                    enemy.ReplicateConVar("sv_disable_radar", "0");
                }
                _ = _restoreTimers.Remove(captured);
            });
            _restoreTimers[attacker] = restoreTimer;

            return HookResult.Continue;
        }
    }
}
