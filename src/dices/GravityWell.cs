using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class GravityWell : DiceBlueprint
    {
        public override string ClassName => "GravityWell";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick"];

        // Static: active gravity wells (position + expiry time)
        internal static readonly List<(Vector Pos, float ExpireTime, string PlayerName)> ActiveWells = [];

        private int _tickCounter;

        public GravityWell(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
                DiceSynergy.AnnounceCombo(player, "引力深渊", "引力持续翻倍");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            ActiveWells.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !_players.Contains(victim)) return HookResult.Continue;
            if (victim.PlayerPawn?.Value?.AbsOrigin == null) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            float duration = _config.Dices.GravityWell.Duration;
            Vector pos = victim.PlayerPawn.Value.AbsOrigin;

            ActiveWells.Add((pos, now + duration, victim.PlayerName));

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_GravityWell_broadcast"].Value.Replace("{playerName}", victim.PlayerName)}");

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0 && ActiveWells.Count == 0) return;
            if (ActiveWells.Count == 0) return;

            _tickCounter++;
            if (_tickCounter % 13 != 0) return;

            float now = (float)Server.CurrentTime;
            float strength = _comboActive
                ? _config.Dices.GravityWell.PullStrength * 2f
                : _config.Dices.GravityWell.PullStrength;
            float radius = _config.Dices.GravityWell.PullRadius;

            // Clean expired wells first
            for (int w = ActiveWells.Count - 1; w >= 0; w--)
            {
                if (now >= ActiveWells[w].ExpireTime)
                    ActiveWells.RemoveAt(w);
            }

            if (ActiveWells.Count == 0) return;

            var entities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("prop_physics_multiplayer")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hegrenade_projectile"))
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("flashbang_projectile"))
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("smokegrenade_projectile"))
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("molotov_projectile"))
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("incendiarygrenade_projectile"))
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("decoy_projectile"))
                .ToArray();

            // Also grab dropped weapon entities
            var weapons = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_ak47")
                .Take(50)
                .ToArray();

            foreach (var well in ActiveWells)
            {
                var (wellPos, _, _) = well;
                float radiusSq = radius * radius;

                var allEnts = entities.Concat(weapons);
                foreach (var ent in allEnts)
                {
                    if (ent?.AbsOrigin == null || !ent.IsValid) continue;

                    float dx = wellPos.X - ent.AbsOrigin.X;
                    float dy = wellPos.Y - ent.AbsOrigin.Y;
                    float dz = wellPos.Z - ent.AbsOrigin.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq > radiusSq || distSq < 1f) continue;

                    float dist = MathF.Sqrt(distSq);
                    float pull = strength * 0.2f * (radius / (dist + 50f));
                    if (pull > dist) pull = dist;

                    float nx = dx / dist;
                    float ny = dy / dist;
                    float nz = dz / dist;

                    Vector newPos = new(
                        ent.AbsOrigin.X + nx * pull,
                        ent.AbsOrigin.Y + ny * pull,
                        ent.AbsOrigin.Z + nz * pull + 5f * 0.2f
                    );

                    QAngle rot = ent.AbsRotation ?? new QAngle(0, 0, 0);
                    Vector vel = ent.AbsVelocity ?? new Vector(0, 0, 0);
                    ent.Teleport(newPos, rot, vel);
                }

                // Pull all alive players toward well
                foreach (var player in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                        && p.PlayerPawn.Value.AbsOrigin != null))
                {
                    CCSPlayerPawn pawn = player.PlayerPawn!.Value!;
                    Vector playerPos = pawn.AbsOrigin!;

                    float dx = wellPos.X - playerPos.X;
                    float dy = wellPos.Y - playerPos.Y;
                    float dz = wellPos.Z - playerPos.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq > radiusSq || distSq < 1f) continue;

                    float dist = MathF.Sqrt(distSq);
                    float pull = strength * 0.06f * (radius / (dist + 50f));
                    if (pull > dist) pull = dist;

                    float nx = dx / dist;
                    float ny = dy / dist;
                    float nz = dz / dist;

                    Vector pullVel = new(nx * pull / 0.2f, ny * pull / 0.2f, nz * pull / 0.2f);
                    pawn.Teleport(null, null, pullVel);
                }
            }
        }
    }
}
