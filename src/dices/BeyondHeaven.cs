using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class BeyondHeaven : DiceBlueprint
    {
        public override string ClassName => "BeyondHeaven";
        public override bool CanBeDrawn => false;
        public override List<string> Listeners => ["OnTick", "OnPlayerButtonsChanged"];

        private readonly Dictionary<CCSPlayerController, float> _timeStopEnd = [];
        private readonly Dictionary<CCSPlayerController, float> _cooldownEnd = [];
        private readonly Dictionary<CCSPlayerController, bool> _used = [];
        private bool _timeStopped;

        public BeyondHeaven(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _used[player] = false;
            _cooldownEnd[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🌌 按E键暂停时间9秒！仅可使用一次！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            EndTimeStop();
            _ = _players.Remove(player);
            _ = _timeStopEnd.Remove(player);
            _ = _cooldownEnd.Remove(player);
            _ = _used.Remove(player);
        }

        public override void Reset()
        {
            EndTimeStop();
            _players.Clear();
            _timeStopEnd.Clear();
            _cooldownEnd.Clear();
            _used.Clear();
            _timeStopped = false;
        }

        public override void Destroy() => Reset();

        private void EndTimeStop()
        {
            if (!_timeStopped) return;
            _timeStopped = false;
            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                MoveLockManager.Unlock(p, "BeyondHeaven");
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float now = (float)Server.CurrentTime;
            if (_cooldownEnd.TryGetValue(player, out float cd) && now < cd) return;
            if (_used.TryGetValue(player, out bool used) && used) return;

            _timeStopEnd[player] = now + _config.Dices.BeyondHeaven.Duration;
            _cooldownEnd[player] = now + _config.Dices.BeyondHeaven.Cooldown;
            _used[player] = true;
            _timeStopped = true;

            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p != player
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                MoveLockManager.Lock(p, "BeyondHeaven");
            }

            player.PrintToCenterAlert("🌌 超越天堂！时间暂停9s！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌌 {player.PlayerName} 超越了天堂！时间暂停9秒！");
        }

        public void OnTick()
        {
            if (!_timeStopped) return;
            float now = (float)Server.CurrentTime;

            foreach (var kv in _timeStopEnd.ToList())
            {
                if (now >= kv.Value)
                {
                    _timeStopEnd.Remove(kv.Key);
                    EndTimeStop();
                    kv.Key?.PrintToCenterAlert("⏰ 时间恢复流动！");
                    return;
                }

                foreach (var p in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV && p != kv.Key
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    MoveLockManager.Lock(p, "BeyondHeaven");
                }

                int remaining = (int)Math.Ceiling(kv.Value - now);
                kv.Key?.PrintToCenterAlert($"🌌 超越天堂！{remaining}s 剩余");
            }
        }
    }
}
