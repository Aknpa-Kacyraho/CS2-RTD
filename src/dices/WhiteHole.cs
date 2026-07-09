using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;
using System.Linq;

namespace RollTheDice.Dices
{
    public class WhiteHole : DiceBlueprint
    {
        public override string ClassName => "WhiteHole";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick"];

        private Vector? _whiteHolePos;
        private float _whiteHoleEndTime;
        private int _ownerTeam;
        private CParticleSystem? _particle;
        private CBeam? _beam;

        // Collapse combo state
        private bool _collapsePull;       // true during 10s pull phase
        private float _collapseEndTime;   // when pull ends and explode happens
        private float _lastCollapseDamage; // for 1s HP drain interval

        private static readonly string[] PushableEntities = [
            "hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile",
            "molotov_projectile", "incendiarygrenade_projectile", "decoy_projectile",
            "weapon_c4",
            "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_awp",
            "weapon_ssg08", "weapon_scar20", "weapon_g3sg1", "weapon_aug",
            "weapon_sg556", "weapon_galilar", "weapon_famas", "weapon_p90",
            "weapon_mp9", "weapon_mac10", "weapon_mp7", "weapon_mp5sd",
            "weapon_ump45", "weapon_bizon", "weapon_nova", "weapon_xm1014",
            "weapon_mag7", "weapon_sawedoff", "weapon_m249", "weapon_negev",
            "weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_glock",
            "weapon_hkp2000", "weapon_p250", "weapon_tec9", "weapon_usp_silencer",
            "weapon_cz75a", "weapon_revolver", "weapon_taser",
            "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade",
            "weapon_molotov", "weapon_incgrenade", "weapon_decoy"
        ];

        public WhiteHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
            : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "BlackHole");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "坍缩", "死时坍缩10s→爆开！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            CleanupWhiteHole();
            _whiteHolePos = null;
            _whiteHoleEndTime = 0;
            _collapsePull = false;
            _collapseEndTime = 0;
            _lastCollapseDamage = 0;
        }

        public override void Destroy() => Reset();

        private void CleanupWhiteHole()
        {
            if (_particle != null && _particle.IsValid)
            {
                _particle.Remove();
            }
            _particle = null;
            if (_beam != null && _beam.IsValid)
            {
                _beam.Remove();
            }
            _beam = null;
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? deadPlayer = @event.Userid;
            if (deadPlayer == null || !deadPlayer.IsValid || !_players.Contains(deadPlayer))
                return HookResult.Continue;
            if (deadPlayer.PlayerPawn?.Value?.AbsOrigin == null)
                return HookResult.Continue;

            // Clean up any existing white hole from previous death
            CleanupWhiteHole();

            CCSPlayerPawn pawn = deadPlayer.PlayerPawn.Value;
            _whiteHolePos = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
            _ownerTeam = deadPlayer.TeamNum;

            if (_comboActive)
            {
                // Collapse: 10s pull then explode
                _collapsePull = true;
                _collapseEndTime = (float)Server.CurrentTime + 10f;
                _lastCollapseDamage = (float)Server.CurrentTime;
                SpawnVisual(_whiteHolePos);
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💥 坍缩开始！10秒后爆开！");
            }
            else
            {
                _whiteHoleEndTime = (float)Server.CurrentTime + _config.Dices.WhiteHole.Duration;
                SpawnVisual(_whiteHolePos);
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_WhiteHole_spawn"].Value.Replace("{playerName}", deadPlayer.PlayerName)}");
            }

            return HookResult.Continue;
        }

        private void SpawnVisual(Vector pos)
        {
            _particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (_particle != null)
            {
                _particle.EffectName = "particles/ui/status_effects/speed_boost.vpcf";
                _particle.StartActive = true;
                _particle.Teleport(pos, new QAngle(), new Vector());
                _particle.DispatchSpawn();
            }

            _beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (_beam != null)
            {
                _beam.Render = Color.FromArgb(200, 255, 255, 255);
                _beam.Width = 12f;
                _beam.Teleport(pos, new QAngle(), new Vector());
                _beam.DispatchSpawn();
            }
        }

        public void OnTick()
        {
            if (_whiteHolePos == null) return;

            float now = (float)Server.CurrentTime;

            // Collapse combo: pull phase → explode
            if (_collapsePull)
            {
                if (now >= _collapseEndTime)
                {
                    // Explode!
                    _collapsePull = false;
                    Vector collapsePos = _whiteHolePos;
                    CleanupWhiteHole();

                    CBaseEntity? boom = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
                    if (boom != null)
                    {
                        boom.Teleport(collapsePos, new QAngle(), new Vector());
                        boom.DispatchSpawn();
                        boom.AcceptInput("Explode");
                    }

                    // Push all players + entities away
                    float pushStrength = 800f;
                    float explodeRadiusSq = 800f * 800f;
                    foreach (var p in Utilities.GetPlayers()
                        .Where(p => p.IsValid && !p.IsHLTV
                            && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && p.PlayerPawn.Value.AbsOrigin != null))
                    {
                        CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                        float dx = pawn.AbsOrigin!.X - collapsePos.X;
                        float dy = pawn.AbsOrigin.Y - collapsePos.Y;
                        float dz = pawn.AbsOrigin.Z - collapsePos.Z;
                        float distSq = dx * dx + dy * dy + dz * dz;
                        if (distSq > explodeRadiusSq || distSq < 1f) continue;
                        float dist = MathF.Sqrt(distSq);
                        float f = pushStrength * (1f - dist / 800f);
                        float nx = dx / dist; float ny = dy / dist; float nz = dz / dist;
                        pawn.Teleport(null, null, new Vector(nx * f, ny * f, nz * f + 200f));
                        pawn.Health -= 50;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        if (pawn.Health <= 0)
                        {
                            if (!p.IsBot && !p.IsHLTV) pawn.CommitSuicide(false, true);
                            else { try { pawn.CommitSuicide(false, true); } catch { pawn.Health = 0; Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth"); } }
                        }
                    }
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💥 坍缩爆炸！");
                    _whiteHolePos = null;
                    return;
                }

                // Pull phase: every 4 ticks
                if (Server.TickCount % 4 != 0) return;

                Vector wh2 = _whiteHolePos;
                float pullRadius = 500f;
                float pullRadiusSq = pullRadius * pullRadius;
                float collapseDt = Server.TickInterval * 4f;

                // Pull entities
                foreach (var typeName in PushableEntities)
                {
                    var entities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(typeName);
                    foreach (var ent in entities)
                    {
                        if (ent?.AbsOrigin == null || !ent.IsValid) continue;
                        if (typeName.StartsWith("weapon_"))
                        {
                            var weapon = ent.As<CBasePlayerWeapon>();
                            if (weapon?.OwnerEntity?.Value != null) continue;
                        }
                        float dx = wh2.X - ent.AbsOrigin.X;
                        float dy = wh2.Y - ent.AbsOrigin.Y;
                        float dz = wh2.Z - ent.AbsOrigin.Z;
                        float distSq = dx * dx + dy * dy + dz * dz;
                        if (distSq > pullRadiusSq || distSq < 1f) continue;
                        float dist = MathF.Sqrt(distSq);
                        float pull = 300f * collapseDt * (1f - dist / pullRadius);
                        float nx = dx / dist; float ny = dy / dist; float nz = dz / dist;
                        ent.Teleport(new Vector(ent.AbsOrigin.X + nx * pull, ent.AbsOrigin.Y + ny * pull, ent.AbsOrigin.Z + nz * pull + 3f * collapseDt), ent.AbsRotation, ent.AbsVelocity);
                    }
                }

                // Pull all players and damage 10HP/s
                if (now - _lastCollapseDamage >= 1f)
                {
                    _lastCollapseDamage = now;
                    foreach (var p in Utilities.GetPlayers()
                        .Where(p => p.IsValid && !p.IsHLTV
                            && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && p.PlayerPawn.Value.AbsOrigin != null))
                    {
                        CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                        float dx = wh2.X - pawn.AbsOrigin!.X;
                        float dy = wh2.Y - pawn.AbsOrigin.Y;
                        float dz = wh2.Z - pawn.AbsOrigin.Z;
                        float distSq = dx * dx + dy * dy + dz * dz;
                        if (distSq > pullRadiusSq || distSq < 1f) continue;
                        float dist = MathF.Sqrt(distSq);
                        float nx = dx / dist; float ny = dy / dist; float nz = dz / dist;
                        float pullForce = 400f * (1f - dist / pullRadius);
                        pawn.Teleport(null, null, new Vector(nx * pullForce / 0.064f, ny * pullForce / 0.064f, nz * pullForce / 0.064f));
                        pawn.Health -= 10;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        if (pawn.Health <= 0)
                        {
                            if (!p.IsBot && !p.IsHLTV) pawn.CommitSuicide(false, true);
                            else { try { pawn.CommitSuicide(false, true); } catch { pawn.Health = 0; Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth"); } }
                        }
                    }
                }
                return;
            }

            if (now >= _whiteHoleEndTime)
            {
                CleanupWhiteHole();
                _whiteHolePos = null;
                return;
            }

            // Throttle to every 4 ticks for performance (~16 times/sec)
            if (Server.TickCount % 4 != 0) return;

            Vector wh = _whiteHolePos;
            float radius = _config.Dices.WhiteHole.PushRadius;
            float radiusSq = radius * radius;
            float enemyPush = _config.Dices.WhiteHole.EnemyPushStrength;
            float teammatePull = _config.Dices.WhiteHole.TeammatePullStrength;
            float entityPush = _config.Dices.WhiteHole.EntityPushStrength;
            float dt = Server.TickInterval * 4f;

            // Push all dropped weapons, projectiles, and C4 away from white hole
            foreach (var typeName in PushableEntities)
            {
                var entities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(typeName);
                foreach (var ent in entities)
                {
                    if (ent?.AbsOrigin == null || !ent.IsValid) continue;

                    // Skip weapons that are being held by a player
                    if (typeName.StartsWith("weapon_"))
                    {
                        var weapon = ent.As<CBasePlayerWeapon>();
                        if (weapon?.OwnerEntity?.Value != null) continue;
                    }

                    float dx = ent.AbsOrigin.X - wh.X;
                    float dy = ent.AbsOrigin.Y - wh.Y;
                    float dz = ent.AbsOrigin.Z - wh.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq > radiusSq || distSq < 1f) continue;

                    float dist = MathF.Sqrt(distSq);
                    float push = entityPush * dt * (1f - dist / radius);
                    if (push > dist) push = dist;

                    float nx = dx / dist;
                    float ny = dy / dist;
                    float nz = dz / dist;

                    Vector newPos = new(
                        ent.AbsOrigin.X + nx * push,
                        ent.AbsOrigin.Y + ny * push,
                        ent.AbsOrigin.Z + nz * push + 3f * dt
                    );

                    ent.Teleport(newPos, ent.AbsRotation, ent.AbsVelocity);
                }
            }

            // Push planted C4 if in range
            var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4")
                .FirstOrDefault(c => c.IsValid && c.AbsOrigin != null);
            if (plantedC4 != null)
            {
                float dx = plantedC4.AbsOrigin!.X - wh.X;
                float dy = plantedC4.AbsOrigin.Y - wh.Y;
                float dz = plantedC4.AbsOrigin.Z - wh.Z;
                float distSq = dx * dx + dy * dy + dz * dz;

                if (distSq <= radiusSq && distSq >= 1f)
                {
                    float dist = MathF.Sqrt(distSq);
                    float push = entityPush * dt * (1f - dist / radius);
                    if (push > dist) push = dist;

                    float nx = dx / dist;
                    float ny = dy / dist;
                    float nz = dz / dist;

                    Vector newPos = new(
                        plantedC4.AbsOrigin.X + nx * push,
                        plantedC4.AbsOrigin.Y + ny * push,
                        plantedC4.AbsOrigin.Z + nz * push + 3f * dt
                    );

                    plantedC4.Teleport(newPos, plantedC4.AbsRotation, plantedC4.AbsVelocity);
                }
            }

            // Push enemies away / pull teammates toward
            foreach (var player in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.PlayerPawn.Value.AbsOrigin != null))
            {
                CCSPlayerPawn pawn = player.PlayerPawn!.Value!;
                Vector playerPos = pawn.AbsOrigin!;

                float dx = playerPos.X - wh.X;
                float dy = playerPos.Y - wh.Y;
                float dz = playerPos.Z - wh.Z;
                float distSq = dx * dx + dy * dy + dz * dz;

                if (distSq > radiusSq || distSq < 1f) continue;

                float dist = MathF.Sqrt(distSq);
                float nx = dx / dist;
                float ny = dy / dist;
                float nz = dz / dist;

                bool isEnemy = player.TeamNum != _ownerTeam;
                float strength = isEnemy ? enemyPush : teammatePull;
                float force = strength * dt * (1f - dist / radius);

                if (isEnemy)
                {
                    // Push away from white hole (positive direction)
                    Vector pushVel = new(nx * force / dt, ny * force / dt, nz * force / dt);
                    pawn.Teleport(null, null, pushVel);
                }
                else
                {
                    // Pull toward white hole (negative direction)
                    Vector pullVel = new(-nx * force / dt, -ny * force / dt, -nz * force / dt);
                    pawn.Teleport(null, null, pullVel);
                }
            }
        }
    }
}
