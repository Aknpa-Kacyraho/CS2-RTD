using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Tactician : DiceBlueprint
    {
        public override string ClassName => "Tactician";
        public override List<string> Listeners => [
            "OnTick"
        ];
        private readonly Dictionary<CCSPlayerController, float> _nextRevealTime = [];
        private readonly Dictionary<CCSPlayerController, List<(CDynamicProp?, CDynamicProp?)>> _activeGlows = [];
        private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _selfGlows = [];

        public Tactician(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _nextRevealTime[player] = 0f;
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextRevealTime.Remove(player);
            RemoveGlowsForPlayer(player);
            RemoveSelfGlow(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                RemoveGlowsForPlayer(player);
                RemoveSelfGlow(player);
            }
            _players.Clear();
            _nextRevealTime.Clear();
            _activeGlows.Clear();
            _selfGlows.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    if (!_nextRevealTime.TryGetValue(player, out float next) || next > now)
                        continue;

                    RevealEnemies(player);
                    _nextRevealTime[player] = now + _config.Dices.Tactician.RevealInterval;
                }
                catch { }
            }
        }

        private void RevealEnemies(CCSPlayerController player)
        {
            RemoveGlowsForPlayer(player);
            RemoveSelfGlow(player);

            List<(CDynamicProp?, CDynamicProp?)> glows = [];

            foreach (var enemy in Utilities.GetPlayers()
                .Where(p => p.IsValid && p.PlayerPawn?.Value?.IsValid == true
                    && p.TeamNum != player.TeamNum
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                var glow = GlowUtil.CreateGlow(enemy.PlayerPawn.Value, Color.Yellow);
                if (glow.Item1 != null && glow.Item2 != null)
                    glows.Add(glow);
            }

            // Self-glow when revealing — the tactician is also exposed
            if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                _selfGlows[player] = GlowUtil.CreateGlow(player.PlayerPawn.Value, Color.Yellow);

            if (glows.Count > 0)
            {
                _activeGlows[player] = glows;

                _ = new Timer(_config.Dices.Tactician.RevealDuration, () =>
                {
                    RemoveGlowsForPlayer(player);
                    RemoveSelfGlow(player);
                });
            }
        }

        private void RemoveGlowsForPlayer(CCSPlayerController player)
        {
            if (_activeGlows.TryGetValue(player, out var glows))
            {
                foreach (var (proxy, glow) in glows)
                    GlowUtil.RemoveGlow(proxy, glow);
                _ = _activeGlows.Remove(player);
            }
        }

        private void RemoveSelfGlow(CCSPlayerController player)
        {
            if (_selfGlows.TryGetValue(player, out var selfGlow))
            {
                GlowUtil.RemoveGlow(selfGlow.Item1, selfGlow.Item2);
                _ = _selfGlows.Remove(player);
            }
        }
    }
}
