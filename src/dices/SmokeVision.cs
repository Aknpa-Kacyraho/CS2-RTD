using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class SmokeVision : DiceBlueprint
    {
        public override string ClassName => "SmokeVision";
        private bool _comboActive;
        public override List<string> Events => [
            "EventSmokegrenadeDetonate"
        ];
        public override List<string> Listeners => [
            "OnTick"
        ];
        private readonly List<CSmokeGrenadeProjectile> _activeSmokes = [];
        private readonly Dictionary<CCSPlayerController, Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)>> _enemyGlows = [];

        public SmokeVision(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

            _comboActive = DiceSynergy.HasPartner(player, "SmokeBomb");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "烟雾掌控", "烟雾穿透加速");
            _enemyGlows[player] = [];
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_enemyGlows.TryGetValue(player, out var glows))
            {
                foreach (var glow in glows.Values)
                {
                    GlowUtil.RemoveGlow(glow.Item1, glow.Item2);
                }
                glows.Clear();
            }
            _ = _players.Remove(player);
            _ = _enemyGlows.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
            _enemyGlows.Clear();
            _activeSmokes.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
        {
            // Find the smoke grenade entity
            CSmokeGrenadeProjectile? smoke = Utilities.FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>("smokegrenade_projectile")
                .FirstOrDefault(s => s.IsValid && s.AbsOrigin != null);
            if (smoke != null)
            {
                _activeSmokes.Add(smoke);
            }
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0 || Server.TickCount % 8 != 0)
            {
                return;
            }

            // Clean up expired smokes
            _activeSmokes.RemoveAll(s => s == null || !s.IsValid || s.AbsOrigin == null);

            Color tColor = Color.FromArgb(
                _config.Dices.SmokeVision.GlowColorTRed,
                _config.Dices.SmokeVision.GlowColorTGreen,
                _config.Dices.SmokeVision.GlowColorTBlue);
            Color ctColor = Color.FromArgb(
                _config.Dices.SmokeVision.GlowColorCTRed,
                _config.Dices.SmokeVision.GlowColorCTGreen,
                _config.Dices.SmokeVision.GlowColorCTBlue);

            foreach (CCSPlayerController player in _players.ToList())
            {
                if (player == null
                    || !player.IsValid
                    || player.Pawn?.Value == null
                    || !player.Pawn.Value.IsValid
                    || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    continue;
                }

                if (!_enemyGlows.ContainsKey(player))
                {
                    _enemyGlows[player] = [];
                }

                var playerGlows = _enemyGlows[player];
                HashSet<CCSPlayerController> enemiesInSmoke = [];

                // Check all enemies
                foreach (CCSPlayerController enemy in Utilities.GetPlayers()
                    .Where(p => p != player
                        && p.IsValid
                        && !p.IsHLTV
                        && p.Pawn?.Value != null
                        && p.Pawn.Value.IsValid
                        && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                        && p.TeamNum != player.TeamNum))
                {
                    // Check if enemy is inside any active smoke
                    bool inSmoke = _activeSmokes.Any(smoke =>
                        smoke.AbsOrigin != null &&
                        enemy.Pawn.Value.AbsOrigin != null &&
                        Vectors.GetDistance(smoke.AbsOrigin, enemy.Pawn.Value.AbsOrigin) < 150f);

                    if (inSmoke)
                    {
                        enemiesInSmoke.Add(enemy);

                        // Add glow if not already glowing
                        if (!playerGlows.ContainsKey(enemy))
                        {
                            var glowPair = GlowUtil.CreateGlow(
                                enemy.Pawn.Value,
                                enemy.TeamNum == (int)CsTeam.Terrorist ? tColor : ctColor);
                            if (glowPair.Item1 != null && glowPair.Item2 != null)
                            {
                                playerGlows[enemy] = glowPair;
                            }
                        }
                    }
                }

                // Remove glow from enemies no longer in smoke
                foreach (var kvp in playerGlows.ToList())
                {
                    if (!enemiesInSmoke.Contains(kvp.Key))
                    {
                        GlowUtil.RemoveGlow(kvp.Value.Item1, kvp.Value.Item2);
                        playerGlows.Remove(kvp.Key);
                    }
                }

                // SmokeBomb combo: speed boost when near any active smoke
                if (_comboActive && _activeSmokes.Count > 0)
                {
                    bool nearSmoke = _activeSmokes.Any(s =>
                        s?.AbsOrigin != null && player.Pawn.Value.AbsOrigin != null &&
                        Vectors.GetDistance(s.AbsOrigin, player.Pawn.Value.AbsOrigin) < 400f);
                    player.PlayerPawn.Value.VelocityModifier = nearSmoke ? 1.5f : 1.0f;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
