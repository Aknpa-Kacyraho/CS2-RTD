using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class FireLord : DiceBlueprint
    {
        public override string ClassName => "FireLord";
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre",
            "OnEntitySpawned",
            "OnTick"
        ];
        private bool _comboActive;
        private readonly Dictionary<CCSPlayerController, float> _nextMolotovTime = [];
        private readonly Dictionary<CCSPlayerController, float> _lastHealTime = [];

        public FireLord(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _nextMolotovTime[player] = (float)Server.CurrentTime + _config.Dices.FireLord.MolotovInterval;
            _lastHealTime[player] = 0f;

            _comboActive = DiceSynergy.HasPartner(player, "Fireball");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "焚天灭地", "10%火焰反噬+炎魔火中15HP/s+30%伤害+每20s燃烧瓶！");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DamageBonusManager.Unregister(player, "FireLord");
            _ = _players.Remove(player);
            _ = _nextMolotovTime.Remove(player);
            _ = _lastHealTime.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) DamageBonusManager.Unregister(p, "FireLord");
            _players.Clear();
            _nextMolotovTime.Clear();
            _lastHealTime.Clear();
        }
        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;
            if ((info.BitsDamageType & DamageTypes_t.DMG_BURN) != 0)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            if (_comboActive)
            {
                CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()
                    ?.Controller?.Value?.As<CCSPlayerController>();
                if (attacker != null && attacker.IsValid)
                {
                    CCSPlayerPawn? aPawn = attacker.PlayerPawn?.Value;
                    if (aPawn != null && aPawn.IsValid && aPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        int reflect = (int)float.Round(info.Damage * 0.1f);
                        if (reflect > 0)
                        {
                            aPawn.Health -= reflect;
                            Utilities.SetStateChanged(aPawn, "CBaseEntity", "m_iHealth");
                            attacker.PrintToCenterAlert($"🔥 火焰反噬 -{reflect}!");
                            if (aPawn.Health <= 0)
                            {
                                if (!attacker.IsBot && !attacker.IsHLTV)
                                    aPawn.CommitSuicide(false, true);
                                else
                                {
                                    try { aPawn.CommitSuicide(false, true); }
                                    catch
                                    {
                                        aPawn.Health = 0;
                                        Utilities.SetStateChanged(aPawn, "CBaseEntity", "m_iHealth");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return HookResult.Continue;
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            string? dn = entity.DesignerName;
            if (dn == null) return;
            if (dn is not ("hegrenade_projectile" or "flashbang_projectile" or "smokegrenade_projectile" or "decoy_projectile"))
                return;

            Server.NextFrame(() =>
            {
                if (entity == null || !entity.IsValid) return;
                CBaseGrenade grenade = new(entity.Handle);
                if (!grenade.IsValid || grenade.AbsOrigin == null) return;

                CCSPlayerPawn? throwerPawn = grenade.OriginalThrower?.Value;
                if (throwerPawn == null || !throwerPawn.IsValid) return;
                CCSPlayerController? thrower = throwerPawn.Controller?.Value?.As<CCSPlayerController>();
                if (thrower == null || !thrower.IsValid || !_players.Contains(thrower)) return;

                Vector pos = new(grenade.AbsOrigin.X, grenade.AbsOrigin.Y, grenade.AbsOrigin.Z);
                Vector vel = new(grenade.AbsVelocity.X, grenade.AbsVelocity.Y, grenade.AbsVelocity.Z);

                grenade.AcceptInput("Kill");

                CMolotovProjectile? molotov = Utilities.CreateEntityByName<CMolotovProjectile>("molotov_projectile");
                if (molotov == null) return;

                molotov.Teleport(pos, new QAngle(0, 0, 0), vel);
                Entities.SetSchemaValue(molotov, "CBaseGrenade", "m_hThrower", throwerPawn.Handle);
                molotov.DispatchSpawn();

                molotov.DetonateTime = 0.01f;
                molotov.AcceptInput("InitializeSpawnFromWorld");

                Server.NextFrame(() =>
                {
                    if (molotov == null || !molotov.IsValid) return;
                    molotov.AcceptInput("Detonate");
                });
            });
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    if (player.PlayerPawn?.Value?.AbsOrigin == null) continue;
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                    if (_nextMolotovTime.TryGetValue(player, out float nextTime) && now >= nextTime)
                    {
                        _nextMolotovTime[player] = now + _config.Dices.FireLord.MolotovInterval;
                        player.GiveNamedItem("weapon_molotov");
                        player.PrintToCenterAlert("🔥 炎魔获得了燃烧瓶！");
                    }

                    bool inFire = false;
                    var infernos = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("inferno");
                    foreach (var inferno in infernos)
                    {
                        if (inferno?.IsValid != true || inferno.AbsOrigin == null) continue;
                        float dist = Vectors.GetDistance(pawn.AbsOrigin, inferno.AbsOrigin);
                        if (dist < 150f)
                        {
                            inFire = true;
                            break;
                        }
                    }

                    if (inFire)
                    {
                        if (_lastHealTime.TryGetValue(player, out float lastHeal) && now - lastHeal >= 1.0f)
                        {
                            _lastHealTime[player] = now;
                            int healAmount = _config.Dices.FireLord.FireHealPerSec;
                            pawn.Health = Math.Min(pawn.Health + healAmount, pawn.MaxHealth);
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        }

                        DamageBonusManager.Register(player, "FireLord", _config.Dices.FireLord.FireDamageBonus);
                    }
                    else
                    {
                        DamageBonusManager.Unregister(player, "FireLord");
                    }

                    if (DamageBonusManager.IsHighest(player, "FireLord"))
                    {
                        float effective = DamageBonusManager.GetEffective(player);
                        // Damage bonus handled in OnPlayerTakeDamagePre via Register/Unregister
                    }
                }
                catch { }
            }
        }
    }
}
