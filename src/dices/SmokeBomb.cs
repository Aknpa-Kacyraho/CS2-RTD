using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class SmokeBomb : DiceBlueprint
    {
        public override string ClassName => "SmokeBomb";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, bool> _usedThisLife = [];

        public SmokeBomb(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "SmokeVision");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "烟雾掌控", "隐形持续翻倍");
            _usedThisLife[player] = false;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _usedThisLife.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _usedThisLife.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_usedThisLife.TryGetValue(victim, out bool used) || used)
                return HookResult.Continue;

            float threshold = _config.Dices.SmokeBomb.HpThresholdPercent;
            int healthAfterDamage = entity.Health - (int)float.Round(info.Damage);
            if (healthAfterDamage > (int)(entity.MaxHealth * threshold))
                return HookResult.Continue;

            info.Damage = entity.Health - 1 > 0 ? entity.Health - 1 : 0;
            _usedThisLife[victim] = true;
            float invisDuration = _comboActive ? 20f : 10f; // combo: double invisibility
            CCSPlayerController capturedVictim = victim;

            Server.NextFrame(() =>
            {
                CCSPlayerPawn? pwn = capturedVictim?.PlayerPawn?.Value;
                if (pwn != null && pwn.IsValid)
                {
                    // Near-invisible during the 10s (alpha 15)
                    pwn.Render = Color.FromArgb(15, 255, 255, 255);
                    Utilities.SetStateChanged(pwn, "CBaseModelEntity", "m_clrRender");
                }

                if (pwn?.AbsOrigin != null)
                {
                    Vector smokePos = new(pwn.AbsOrigin.X, pwn.AbsOrigin.Y, pwn.AbsOrigin.Z + 5);
                    var smoke = Utilities.CreateEntityByName<CSmokeGrenadeProjectile>("smokegrenade_projectile");
                    if (smoke != null && smoke.IsValid)
                    {
                        smoke.Teleport(smokePos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                        smoke.DispatchSpawn();
                        smoke.SmokeColor.X = 200;
                        smoke.SmokeColor.Y = 200;
                        smoke.SmokeColor.Z = 200;
                        var pawnHandle = capturedVictim?.PlayerPawn?.Value?.Handle;
                        if (pawnHandle.HasValue)
                        {
                            Entities.SetSchemaValue(smoke, "CBaseGrenade", "m_hThrower", pawnHandle.Value);
                        }
                        Entities.SetSchemaValue(smoke, "CSmokeGrenadeProjectile", "m_vSmokeDetonationPos", smokePos);
                        smoke.DetonateTime = 0f;
                        smoke.AcceptInput("InitializeSpawnFromWorld");
                        smoke.AcceptInput("Detonate");
                        Server.NextFrame(() =>
                        {
                            if (smoke != null && smoke.IsValid)
                            {
                                smoke.DetonateTime = 0f;
                                smoke.AcceptInput("Detonate");
                            }
                        });
                    }
                }

                capturedVictim?.PrintToCenterAlert(_comboActive ? "💨 迷雾逃生！20秒隐身！" : "💨 迷雾逃生！10秒隐身！");
            });

            new CounterStrikeSharp.API.Modules.Timers.Timer(invisDuration, () =>
            {
                CCSPlayerPawn? pwn = capturedVictim?.PlayerPawn?.Value;
                if (pwn != null && pwn.IsValid)
                {
                    pwn.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(pwn, "CBaseModelEntity", "m_clrRender");
                }
            });

            return HookResult.Changed;
        }
    }
}
