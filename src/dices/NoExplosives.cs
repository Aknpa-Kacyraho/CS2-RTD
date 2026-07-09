using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class NoExplosives : DiceBlueprint
    {
        public override string ClassName => "NoExplosives";
        public override List<string> Listeners => [
            "OnEntitySpawned",
            "OnEntityTakeDamagePre"
        ];
        private readonly HashSet<string> _grenadeProjectiles =
        [
            "smokegrenade_projectile",
            "hegrenade_projectile",
            "molotov_projectile",
            "decoy_projectile",
            "flashbang_projectile"
        ];
        public readonly Random _random = new();
        private readonly Dictionary<nint, CCSPlayerController> _grenadesThrownByPlayers = [];

        public NoExplosives(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;

            // Pick 2 random enemies to disable their explosives
            Server.NextFrame(() =>
            {
                var enemies = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.TeamNum != player.TeamNum
                        && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                        && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .ToList();

                if (enemies.Count == 0) return;

                var rng = new Random();
                int count = Math.Min(2, enemies.Count);
                var targets = enemies.OrderBy(_ => rng.Next()).Take(count).ToList();

                foreach (var enemy in targets)
                {
                    if (enemy != null && enemy.IsValid)
                    {
                        _players.Add(enemy);
                        NotifyPlayers(enemy, ClassName, new() { { "playerName", enemy.PlayerName } });
                    }
                }
            });

            // Notify the roller (self is not affected)
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _grenadesThrownByPlayers.Clear();
        }

        public override void Destroy() => Reset();

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (_grenadeProjectiles.Contains(entity.DesignerName))
            {
                DiceNoExplosivesHandle(entity.Handle);
            }
        }

        public HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity is null
                || !entity.IsValid
                || info.Inflictor is null
                || !info.Inflictor.IsValid
                || info.Inflictor.Value is null
                || !info.Inflictor.Value.IsValid
                || !_grenadesThrownByPlayers.ContainsKey(info.Inflictor.Value.Handle))
            {
                return HookResult.Continue;
            }
            nint inflictorHandler = info.Inflictor.Value.Handle;
            info.Attacker.Raw = _grenadesThrownByPlayers[inflictorHandler].Pawn.Raw;
            info.BitsDamageType = DamageTypes_t.DMG_HEADSHOT;
            return HookResult.Changed;
        }

        private void DiceNoExplosivesHandle(nint handle)
        {
            Server.NextFrame(() =>
            {
                if (handle == IntPtr.Zero) return;

                CBaseGrenade grenade = new(handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero || grenade.AbsOrigin == null) return;
                CCSPlayerPawn? owner = grenade.OriginalThrower?.Value;
                if (owner == null || !owner.IsValid) return;
                if (owner.Controller?.Value != null && _players.Contains(owner.Controller.Value))
                {
                    if (_config.Dices.NoExplosives.RandomModels.Count == 0) return;
                    string Model = _config.Dices.NoExplosives.RandomModels[new Random().Next(_config.Dices.NoExplosives.RandomModels.Count)];
                    nint inflictorHandler = CreatePhysicsModel(
                        Model,
                        _config.Dices.NoExplosives.ModelScale,
                        grenade.AbsOrigin,
                        new QAngle(0, 0, 0),
                        new Vector(
                            grenade.Velocity.X,
                            grenade.Velocity.Y,
                            grenade.Velocity.Z
                        ));
                    _grenadesThrownByPlayers.Add(
                        inflictorHandler,
                        owner.Controller.Value.As<CCSPlayerController>());
                    _ = new CounterStrikeSharp.API.Modules.Timers.Timer(10f, () =>
                    {
                        _ = _grenadesThrownByPlayers.Remove(inflictorHandler);
                    });
                    _ = grenade.EmitSound("StopSoundEvents.StopAllExceptMusic");
                    grenade.AcceptInput("Kill");
                }
            });
        }

        private static nint CreatePhysicsModel(string model, float scale, Vector origin, QAngle angles, Vector velocity)
        {
            CPhysicsProp prop;
            prop = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_multiplayer");
            if (prop == null) return 0;
            prop.Health = 10;
            prop.MaxHealth = 10;
            CEntityKeyValues kv = new();
            kv.SetFloat("modelscale", scale);
            prop.SetModel(model);
            prop.DispatchSpawn(kv);
            prop.Teleport(origin, angles, velocity);
            return prop.Handle;
        }
    }
}
