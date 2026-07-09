using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class BlackHole : DiceBlueprint
    {
        public override string ClassName => "BlackHole";
        private bool _comboActive;
        private bool _collapseCombo;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick"];

        private int _tickCounter;

        private static readonly string[] PullableEntities = [
            "hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile",
            "molotov_projectile", "incendiarygrenade_projectile", "decoy_projectile",
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

        public BlackHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "GravityWell");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "引力深渊", "黑洞吸入玩家+武器，半径翻倍！");
            _collapseCombo = DiceSynergy.HasPartner(player, "WhiteHole");
            if (_collapseCombo)
                DiceSynergy.AnnounceCombo(player, "坍缩", "死时坍缩10s→爆开！");

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.Render = Color.FromArgb(255, 0, 0, 0);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                if (p?.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                }
            }
            _players.Clear();
        }

        public override void Destroy() => Reset();

        // WhiteHole combo: on death, collapse for 10s then explode
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;
            if (!_collapseCombo) return HookResult.Continue;
            if (victim.PlayerPawn?.Value?.AbsOrigin == null) return HookResult.Continue;

            Vector deathPos = new(victim.PlayerPawn.Value.AbsOrigin.X,
                victim.PlayerPawn.Value.AbsOrigin.Y, victim.PlayerPawn.Value.AbsOrigin.Z);

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💥 坍缩开始！10秒后黑洞爆开！");
            Server.NextFrame(() =>
            {
                // Create a growing visual beam at death site
                var beam = Utilities.CreateEntityByName<CBeam>("beam");
                if (beam != null && beam.IsValid)
                {
                    beam.Render = Color.FromArgb(255, 100, 0, 200);
                    beam.Width = 4f;
                    beam.Teleport(deathPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    beam.EndPos.X = deathPos.X;
                    beam.EndPos.Y = deathPos.Y;
                    beam.EndPos.Z = deathPos.Z + 50;
                    beam.DispatchSpawn();
                    // Fade beam after 10s
                    var capturedBeam = beam;
                    new CounterStrikeSharp.API.Modules.Timers.Timer(9.5f, () =>
                    {
                        if (capturedBeam != null && capturedBeam.IsValid) capturedBeam.Remove();
                    });
                }
            });

            // After 10s, create explosion at death location
            var capPos = deathPos;
            new CounterStrikeSharp.API.Modules.Timers.Timer(10f, () =>
            {
                var explosion = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
                if (explosion != null && explosion.IsValid)
                {
                    explosion.Teleport(capPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    explosion.DispatchSpawn();
                    explosion.AcceptInput("Explode");
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💥 坍缩！黑洞爆开了！");
                }
            });

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            _tickCounter++;
            if (_tickCounter % 16 != 0) return;

            float radius = _comboActive ? _config.Dices.BlackHole.PullRadius * 2f : _config.Dices.BlackHole.PullRadius;
            float strength = _config.Dices.BlackHole.PullStrength;
            float dt = 0.25f;

            foreach (var player in _players.ToList())
            {
                if (player?.PlayerPawn?.Value?.AbsOrigin == null) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                Vector playerPos = player.PlayerPawn.Value.AbsOrigin;
                float radiusSq = radius * radius;

                foreach (var typeName in PullableEntities)
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

                        float dx = playerPos.X - ent.AbsOrigin.X;
                        float dy = playerPos.Y - ent.AbsOrigin.Y;
                        float dz = playerPos.Z - ent.AbsOrigin.Z;
                        float distSq = dx * dx + dy * dy + dz * dz;

                        if (distSq > radiusSq || distSq < 1f) continue;

                        float dist = MathF.Sqrt(distSq);
                        float pull = strength * dt * (1f - dist / radius);
                        if (pull > dist) pull = dist;

                        float nx = dx / dist;
                        float ny = dy / dist;
                        float nz = dz / dist;

                        Vector newPos = new(
                            ent.AbsOrigin.X + nx * pull,
                            ent.AbsOrigin.Y + ny * pull,
                            ent.AbsOrigin.Z + nz * pull + 3f * dt
                        );

                        ent.Teleport(newPos, ent.AbsRotation, ent.AbsVelocity);
                    }
                }

                foreach (var enemy in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV && p != player
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                        && p.PlayerPawn.Value.AbsOrigin != null))
                {
                    Vector enemyPos = enemy.PlayerPawn!.Value!.AbsOrigin!;
                    float dx = playerPos.X - enemyPos.X;
                    float dy = playerPos.Y - enemyPos.Y;
                    float dz = playerPos.Z - enemyPos.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq > radiusSq || distSq < 1f) continue;

                    float dist = MathF.Sqrt(distSq);
                    float pull = strength * 0.3f * dt * (1f - dist / radius);
                    if (pull > dist) pull = dist;

                    float nx = dx / dist;
                    float ny = dy / dist;
                    float nz = dz / dist;

                    Vector pullVel = new(nx * pull / dt, ny * pull / dt, nz * pull / dt);
                    enemy.PlayerPawn.Value.Teleport(null, null, pullVel);
                    if (Server.TickCount % 128 == 0)
                        enemy.PrintToCenterAlert("🌌 被黑洞引力吸入！");
                }
            }
        }
    }
}
