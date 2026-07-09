using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Drone : DiceBlueprint
    {
        public override string ClassName => "Drone";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick"];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        // Per-player drone state
        private readonly Dictionary<CCSPlayerController, CDynamicProp> _droneProps = [];
        private readonly Dictionary<CCSPlayerController, CBeam> _tetherBeams = [];
        private readonly Dictionary<CCSPlayerController, float> _orbitAngles = [];
        private readonly Dictionary<CCSPlayerController, float> _lastFireTime = [];

        // Track active shooting beams to clean them up
        private readonly List<CBeam> _shotBeams = [];

        public Drone(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "RepulsionField") || DiceSynergy.HasPartner(player, "Satellite");
            if (DiceSynergy.HasPartner(player, "RepulsionField"))
                DiceSynergy.AnnounceCombo(player, "无人防线", "斥力半径翻倍");
            if (DiceSynergy.HasPartner(player, "Satellite"))
                DiceSynergy.AnnounceCombo(player, "天网", "无人机伤害+50%！卫星浮空！");
            _orbitAngles[player] = (float)(_random.NextDouble() * MathF.PI * 2);
            _lastFireTime[player] = 0f;
            SpawnDrone(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DestroyDrone(player);
            _ = _players.Remove(player);
            _ = _orbitAngles.Remove(player);
            _ = _lastFireTime.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) DestroyDrone(p);
            _players.Clear(); _orbitAngles.Clear(); _lastFireTime.Clear();
        }

        public override void Destroy()
        {
            // Clean all active shot beams — do this after Reset()
            // because Reset may add more players in edge cases
            foreach (var b in _shotBeams.ToList())
                if (b != null && b.IsValid) b.Remove();
            _shotBeams.Clear();
            Reset();
        }

        private void SpawnDrone(CCSPlayerController player)
        {
            DestroyDrone(player);

            CDynamicProp drone = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (drone == null) return;

            drone.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            drone.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            if (drone.CBodyComponent?.SceneNode?.Owner?.Entity != null)
                drone.CBodyComponent.SceneNode.Owner.Entity.Flags &= ~(1u << 2);
            drone.DispatchSpawn();
            drone.Render = Color.FromArgb(255, 100, 220, 255);
            Utilities.SetStateChanged(drone, "CBaseModelEntity", "m_clrRender");
            var skeleton = drone.CBodyComponent?.SceneNode?.GetSkeletonInstance();
            if (skeleton != null) skeleton.Scale = 0.5f;

            _droneProps[player] = drone;

            // Tether beam from player to drone
            CBeam tether = Utilities.CreateEntityByName<CBeam>("beam");
            if (tether != null)
            {
                tether.Render = Color.FromArgb(80, 80, 200, 255);
                tether.Width = 0.8f;
                tether.DispatchSpawn();
                _tetherBeams[player] = tether;
            }
        }

        private void DestroyDrone(CCSPlayerController player)
        {
            if (_droneProps.TryGetValue(player, out var drone))
            {
                if (drone != null && drone.IsValid) drone.Remove();
                _ = _droneProps.Remove(player);
            }
            if (_tetherBeams.TryGetValue(player, out var tether))
            {
                if (tether != null && tether.IsValid) tether.Remove();
                _ = _tetherBeams.Remove(player);
            }
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            // Always clean expired shot beams — even if no players are active.
            // Otherwise beams from before a round-end Reset() would never be removed.
            for (int i = _shotBeams.Count - 1; i >= 0; i--)
            {
                var b = _shotBeams[i];
                if (b == null || !b.IsValid) { _shotBeams.RemoveAt(i); }
                else
                {
                    b.Width -= Server.TickInterval * 8f;
                    if (b.Width <= 0.1f) { b.Remove(); _shotBeams.RemoveAt(i); }
                }
            }

            if (_players.Count == 0) return;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    if (player.PlayerPawn?.Value?.AbsOrigin == null) continue;
                    if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    if (!_droneProps.TryGetValue(player, out var drone) || drone == null || !drone.IsValid) continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    Vector origin = pawn.AbsOrigin;
                    float radius = _config.Dices.Drone.OrbitRadius;
                    float height = _config.Dices.Drone.OrbitHeight;
                    float speed = _config.Dices.Drone.OrbitSpeed;

                    // Update orbit angle
                    float angle = _orbitAngles[player] + speed * Server.TickInterval;
                    _orbitAngles[player] = angle;

                    // Drone position
                    Vector dronePos = new(
                        origin.X + MathF.Cos(angle) * radius,
                        origin.Y + MathF.Sin(angle) * radius,
                        origin.Z + height);
                    drone.Teleport(dronePos, new QAngle(), new Vector());

                    // Update tether
                    if (_tetherBeams.TryGetValue(player, out var tether) && tether != null && tether.IsValid)
                    {
                        tether.Teleport(new Vector(origin.X, origin.Y, origin.Z + 64f), new QAngle(), new Vector());
                        tether.EndPos.X = dronePos.X;
                        tether.EndPos.Y = dronePos.Y;
                        tether.EndPos.Z = dronePos.Z;
                    }

                    // Fire if cooldown ready
                    float fireRate = _config.Dices.Drone.FireRate;
                    if (!_lastFireTime.TryGetValue(player, out float lastFire) || now - lastFire < fireRate)
                        continue;

                    float attackRange = _config.Dices.Drone.AttackRange;
                    CCSPlayerController? bestTarget = null;
                    float bestDist = attackRange;

                    foreach (var enemy in Utilities.GetPlayers()
                        .Where(p => p.IsValid && !p.IsHLTV
                            && p.TeamNum != player.TeamNum && p != player
                            && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                            && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && p.Pawn.Value.AbsOrigin != null))
                    {
                        float dx = enemy.Pawn!.Value!.AbsOrigin!.X - dronePos.X;
                        float dy = enemy.Pawn.Value.AbsOrigin.Y - dronePos.Y;
                        float dz = enemy.Pawn.Value.AbsOrigin.Z - dronePos.Z;
                        float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestTarget = enemy;
                        }
                    }

                    if (bestTarget != null)
                    {
                        _lastFireTime[player] = now;
                        Vector targetPos = bestTarget.Pawn!.Value!.AbsOrigin!;

                        // Shooting beam
                        CBeam shotBeam = Utilities.CreateEntityByName<CBeam>("beam");
                        if (shotBeam != null)
                        {
                            shotBeam.Render = Color.FromArgb(255, 255, 60, 60);
                            shotBeam.Width = 2f;
                            shotBeam.Teleport(dronePos, new QAngle(), new Vector());
                            shotBeam.EndPos.X = targetPos.X;
                            shotBeam.EndPos.Y = targetPos.Y;
                            shotBeam.EndPos.Z = targetPos.Z;
                            shotBeam.DispatchSpawn();
                            _shotBeams.Add(shotBeam);
                        }

                        // Damage
                        int dmg = _random.Next(_config.Dices.Drone.DamageMin,
                            _config.Dices.Drone.DamageMax + 1);
                        if (_comboActive) dmg = (int)float.Round(dmg * 1.5f);
                        CCSPlayerPawn ePawn = bestTarget.PlayerPawn.Value;
                        ePawn.Health -= dmg;
                        Utilities.SetStateChanged(ePawn, "CBaseEntity", "m_iHealth");
                        if (ePawn.Health <= 0 && !bestTarget.IsBot)
                            ePawn.CommitSuicide(false, true);

                        player.PrintToCenterAlert($"🔫 -{dmg} → {bestTarget.PlayerName}");
                    }
                }
                catch { }
            }
        }
    }
}
