using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Singularity : DiceBlueprint
    {
        public override string ClassName => "Singularity";
        public override List<string> Listeners => ["OnTick", "OnPlayerButtonsChanged"];

        public override float GetCooldownRemaining(CCSPlayerController player)
            => _players.Contains(player) ? Math.Max(0, _nextUseTime - (float)Server.CurrentTime) : 0f;

        private Vector? _singularityPos;
        private float _singularityEndTime;
        private float _nextUseTime;
        private CParticleSystem? _particle;
        private CBeam? _glowBall;
        private bool _countdown10Shown;
        private bool _countdown5Shown;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Singularity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🌀 按E键释放奇点！凝聚坍缩一切！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            CleanupSingularity();
        }

        public override void Reset()
        {
            _players.Clear();
            CleanupSingularity();
            _singularityPos = null;
            _singularityEndTime = 0;
            _nextUseTime = 0;
            _countdown10Shown = false;
            _countdown5Shown = false;
        }

        public override void Destroy() => Reset();

        private void CleanupSingularity()
        {
            if (_particle != null && _particle.IsValid)
                _particle.Remove();
            _particle = null;
            if (_glowBall != null && _glowBall.IsValid)
                _glowBall.Remove();
            _glowBall = null;
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float now = (float)Server.CurrentTime;
            if (now < _nextUseTime) return;
            _nextUseTime = now + _config.Dices.Singularity.Cooldown;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            if (pawn.AbsOrigin == null) return;

            Vector eyePos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 64f);
            QAngle eyeAngles = pawn.EyeAngles;
            float yawRad = eyeAngles.Y * MathF.PI / 180f;
            float pitchRad = eyeAngles.X * MathF.PI / 180f;
            Vector forward = new(
                MathF.Cos(pitchRad) * MathF.Cos(yawRad),
                MathF.Cos(pitchRad) * MathF.Sin(yawRad),
                -MathF.Sin(pitchRad));

            Vector aimPoint = eyePos + forward * 2000f;

            _singularityPos = aimPoint;
            _singularityEndTime = now + _config.Dices.Singularity.Duration;
            _countdown10Shown = false;
            _countdown5Shown = false;

            SpawnSingularityVisual(aimPoint);

            player.PrintToCenterAlert($"🌀 奇点释放！{_config.Dices.Singularity.Duration:F0}秒");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Singularity_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
        }

        private void SpawnSingularityVisual(Vector pos)
        {
            CleanupSingularity();

            _particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (_particle != null)
            {
                _particle.EffectName = "particles/ui/status_effects/speed_boost.vpcf";
                _particle.StartActive = true;
                _particle.Teleport(pos, new QAngle(), new Vector());
                _particle.DispatchSpawn();
            }

            _glowBall = Utilities.CreateEntityByName<CBeam>("beam");
            if (_glowBall != null)
            {
                _glowBall.Render = Color.FromArgb(200, 100, 50, 200);
                _glowBall.Width = 8f;
                _glowBall.Teleport(pos, new QAngle(), new Vector());
                _glowBall.DispatchSpawn();
            }
        }

        public void OnTick()
        {
            if (_singularityPos == null) return;
            float now = (float)Server.CurrentTime;

            if (now >= _singularityEndTime)
            {
                CleanupSingularity();
                _singularityPos = null;
                _countdown10Shown = false;
                _countdown5Shown = false;
                return;
            }

            float remaining = _singularityEndTime - now;
            if (!_countdown10Shown && remaining <= 10f)
            {
                _countdown10Shown = true;
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV))
                    p.PrintToCenterAlert("🌀 奇点释放！10秒");
            }
            if (!_countdown5Shown && remaining <= 5f)
            {
                _countdown5Shown = true;
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV))
                    p.PrintToCenterAlert("🌀 奇点释放！5秒");
            }

            if (_singularityPos == null) return;
            Vector sp = _singularityPos;
            float strength = _config.Dices.Singularity.PullStrength;

            foreach (var player in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.PlayerPawn.Value.AbsOrigin != null))
            {
                CCSPlayerPawn pawn = player.PlayerPawn!.Value;
                Vector playerPos = pawn.AbsOrigin!;

                float dx = sp.X - playerPos.X;
                float dy = sp.Y - playerPos.Y;
                float dz = sp.Z - playerPos.Z;
                float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                if (dist < 20f) continue;

                float nx = dx / dist;
                float ny = dy / dist;
                float nz = dz / dist;

                float pullForce = strength * Server.TickInterval * (1000f / (dist + 200f));
                if (pullForce > dist) pullForce = dist;

                Vector vel = new(
                    nx * pullForce / Server.TickInterval,
                    ny * pullForce / Server.TickInterval,
                    nz * pullForce / Server.TickInterval);

                pawn.Teleport(null, null, vel);
            }
        }
    }
}
