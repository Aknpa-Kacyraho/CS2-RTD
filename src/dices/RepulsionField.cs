using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class RepulsionField : DiceBlueprint
    {
        public override string ClassName => "RepulsionField";
        private bool _comboActive;
        public override List<string> Listeners => ["OnEntitySpawned", "OnTick"];
        private readonly HashSet<string> _projectileTypes =
        [   "smokegrenade_projectile", "hegrenade_projectile",
            "molotov_projectile", "decoy_projectile", "flashbang_projectile"  ];
        private readonly Dictionary<uint, float> _trackedNades = [];

        private const float MetersToUnits = 39.37f;

        public RepulsionField(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Drone") || DiceSynergy.HasPartner(player, "MagneticPulse");
            if (DiceSynergy.HasPartner(player, "Drone"))
                DiceSynergy.AnnounceCombo(player, "无人防线", "斥力半径翻倍");
            if (DiceSynergy.HasPartner(player, "MagneticPulse"))
                DiceSynergy.AnnounceCombo(player, "禁区", "斥力场弹回投掷物+磁力脉冲缴械！完全封锁远程！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset() { _players.Clear(); _trackedNades.Clear(); }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (!_projectileTypes.Contains(entity.DesignerName)) return;

            Server.NextFrame(() =>
            {
                if (entity == null || !entity.IsValid) return;
                var nade = new CBaseCSGrenadeProjectile(entity.Handle);
                if (!nade.IsValid) return;
                _trackedNades[nade.Index] = (float)Server.CurrentTime + 10f;
            });
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            foreach (var kv in _trackedNades.ToList())
                if (now > kv.Value) _trackedNades.Remove(kv.Key);

            if (_players.Count == 0) return;
            if (Server.TickCount % 4 != 0) return;

            float radiusMeters = _comboActive ? _config.Dices.RepulsionField.Radius * 2f : _config.Dices.RepulsionField.Radius;
            float radius = radiusMeters * MetersToUnits;

            foreach (var kv in _trackedNades.ToList())
            {
                var nade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)kv.Key);
                if (nade == null || !nade.IsValid || nade.AbsOrigin == null) { _trackedNades.Remove(kv.Key); continue; }

                foreach (var player in _players)
                {
                    if (player?.PlayerPawn?.Value?.AbsOrigin == null) continue;

                    float dx = nade.AbsOrigin.X - player.PlayerPawn.Value.AbsOrigin.X;
                    float dy = nade.AbsOrigin.Y - player.PlayerPawn.Value.AbsOrigin.Y;
                    float dz = nade.AbsOrigin.Z - player.PlayerPawn.Value.AbsOrigin.Z;
                    float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (dist < radius && (nade.TeamNum != player.TeamNum || nade.TeamNum == 0))
                    {
                        float mult = _config.Dices.RepulsionField.SpeedMultiplier;
                        Vector reversed = new(-nade.AbsVelocity.X * mult, -nade.AbsVelocity.Y * mult, -nade.AbsVelocity.Z * mult * 0.5f);
                        nade.Teleport(null, null, reversed);
                        _trackedNades.Remove(kv.Key);
                        break;
                    }
                }
            }
        }
    }
}
