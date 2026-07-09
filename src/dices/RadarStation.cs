using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class RadarStation : DiceBlueprint
    {
        public override string ClassName => "RadarStation";
        public override bool CanBeDrawn => false;
        public override List<string> Listeners => [
            "OnTick"
        ];
        private readonly Dictionary<CCSPlayerController, Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)>> _enemyGlows = [];
        private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _selfGlows = [];

        public RadarStation(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _enemyGlows[player] = [];

            ApplyEnemyGlows(player);

            // Dice owner also glows orange — visible through walls to all
            if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                _selfGlows[player] = GlowUtil.CreateGlow(player.PlayerPawn.Value, Color.Orange);
                // 0.6x speed
                player.PlayerPawn.Value.VelocityModifier = 0.6f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_enemyGlows.TryGetValue(player, out var glows))
            {
                foreach (var kvp in glows)
                    GlowUtil.RemoveGlow(kvp.Value.Item1, kvp.Value.Item2);
                glows.Clear();
            }
            _ = _enemyGlows.Remove(player);
            if (_selfGlows.TryGetValue(player, out var selfGlow))
            {
                GlowUtil.RemoveGlow(selfGlow.Item1, selfGlow.Item2);
                _ = _selfGlows.Remove(player);
            }
            _ = _players.Remove(player);

            // Reset speed
            if (player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                if (_enemyGlows.TryGetValue(player, out var glows))
                {
                    foreach (var kvp in glows)
                        GlowUtil.RemoveGlow(kvp.Value.Item1, kvp.Value.Item2);
                }
                if (_selfGlows.TryGetValue(player, out var selfGlow))
                    GlowUtil.RemoveGlow(selfGlow.Item1, selfGlow.Item2);
            }
            _players.Clear();
            _enemyGlows.Clear();
            _selfGlows.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            foreach (var diceOwner in _players.ToList())
            {
                try
                {
                    if (diceOwner == null
                        || !diceOwner.IsValid
                        || diceOwner.PlayerPawn?.Value == null
                        || !diceOwner.PlayerPawn.Value.IsValid
                        || diceOwner.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        continue;
                    }

                    // Maintain 0.6x speed
                    diceOwner.PlayerPawn.Value.VelocityModifier = 0.6f;
                    Utilities.SetStateChanged(diceOwner.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");

                    if (!_enemyGlows.TryGetValue(diceOwner, out var glowDict))
                        continue;

                    var activeEnemies = new HashSet<CCSPlayerController>();

                    foreach (var enemy in Utilities.GetPlayers()
                        .Where(p => p.IsValid
                            && p.TeamNum != diceOwner.TeamNum
                            && p.PlayerPawn?.Value != null
                            && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                    {
                        activeEnemies.Add(enemy);

                        if (!glowDict.ContainsKey(enemy))
                        {
                            Color glowColor = enemy.TeamNum == (int)CsTeam.Terrorist ? Color.Red : Color.Blue;
                            glowDict[enemy] = GlowUtil.CreateGlow(enemy.PlayerPawn!.Value!, glowColor);
                        }
                    }

                    foreach (var trackedEnemy in glowDict.Keys.ToList())
                    {
                        if (!activeEnemies.Contains(trackedEnemy))
                        {
                            var glow = glowDict[trackedEnemy];
                            GlowUtil.RemoveGlow(glow.Item1, glow.Item2);
                            glowDict.Remove(trackedEnemy);
                        }
                    }
                }
                catch { }
            }
        }

        private void ApplyEnemyGlows(CCSPlayerController diceOwner)
        {
            if (!_enemyGlows.TryGetValue(diceOwner, out var glowDict))
                return;

            foreach (var enemy in Utilities.GetPlayers()
                .Where(p => p.IsValid
                    && p.TeamNum != diceOwner.TeamNum
                    && p.PlayerPawn?.Value != null
                    && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                if (!glowDict.ContainsKey(enemy))
                {
                    Color glowColor = enemy.TeamNum == (int)CsTeam.Terrorist ? Color.Red : Color.Blue;
                    glowDict[enemy] = GlowUtil.CreateGlow(enemy.PlayerPawn!.Value!, glowColor);
                }
            }
        }
    }
}
