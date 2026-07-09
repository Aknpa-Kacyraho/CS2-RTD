using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class LaserCage : DiceBlueprint
    {
        public override string ClassName => "LaserCage";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, List<CBeam>> _activeBeams = [];
        private readonly Dictionary<CCSPlayerController, float> _lastDamageTime = [];
        private static readonly HashSet<string> PistolWeapons = [
            "weapon_glock", "weapon_usp_silencer", "weapon_p250", "weapon_deagle",
            "weapon_elite", "weapon_fiveseven", "weapon_tec9", "weapon_hkp2000",
            "weapon_cz75a", "weapon_revolver"
        ];
        private float _rotationAngle;

        public LaserCage(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            DamageReductionManager.Register(player, "LaserCage", 0.30f);

            _comboActive = DiceSynergy.HasPartner(player, "ThunderChain");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "雷光炼狱", "免疫手枪+30%枪械减伤，激光伤害翻倍！");
            _lastDamageTime[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DestroyBeams(player);
            DamageReductionManager.Unregister(player, "LaserCage");
            _ = _players.Remove(player);
            _ = _lastDamageTime.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                DestroyBeams(player);
                DamageReductionManager.Unregister(player, "LaserCage");
            }
            _players.Clear(); _lastDamageTime.Clear();
        }

        public override void Destroy() => Reset();

        private void DestroyBeams(CCSPlayerController player)
        {
            if (_activeBeams.TryGetValue(player, out var beams))
            {
                foreach (var beam in beams)
                    if (beam != null && beam.IsValid) beam.Remove();
                beams.Clear();
                _ = _activeBeams.Remove(player);
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            string? weaponName = info.Inflictor?.Value?.DesignerName;
            if (weaponName != null && PistolWeapons.Contains(weaponName))
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            if ((info.BitsDamageType & DamageTypes_t.DMG_BULLET) != 0)
            {
                float reduction = DamageReductionManager.GetEffective(victim, 0.30f);
                info.Damage = (int)(info.Damage * (1 - reduction));
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;
            _rotationAngle += 1.5f * Server.TickInterval;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    if (player.PlayerPawn?.Value?.AbsOrigin == null) continue;
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) { DestroyBeams(player); continue; }

                    float radius = _config.Dices.LaserCage.Radius;
                    int count = _config.Dices.LaserCage.BeamCount;
                    Vector origin = pawn.AbsOrigin;

                    DestroyBeams(player);
                    _activeBeams[player] = [];

                    for (int i = 0; i < count; i++)
                    {
                        float angle = _rotationAngle + i * 2f * MathF.PI / count;
                        float bx = origin.X + MathF.Cos(angle) * radius;
                        float by = origin.Y + MathF.Sin(angle) * radius;
                        float tx = origin.X + MathF.Cos(angle + 0.3f) * radius;
                        float ty = origin.Y + MathF.Sin(angle + 0.3f) * radius;

                        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
                        if (beam != null && beam.IsValid)
                        {
                            beam.Render = Color.FromArgb(255, 255, 50, 50);
                            beam.Width = 1.5f;
                            beam.Teleport(new Vector(bx, by, origin.Z), new QAngle(), new Vector());
                            beam.EndPos.X = tx;
                            beam.EndPos.Y = ty;
                            beam.EndPos.Z = origin.Z + 90f;
                            beam.DispatchSpawn();
                            _activeBeams[player].Add(beam);
                        }
                    }

                    float tickInterval = _config.Dices.LaserCage.TickInterval;
                    if (_lastDamageTime.TryGetValue(player, out float lastDmg) && now - lastDmg < tickInterval)
                        continue;
                    _lastDamageTime[player] = now;

                    int damage = _comboActive ? _config.Dices.LaserCage.DamagePerTouch * 2 : _config.Dices.LaserCage.DamagePerTouch;
                    foreach (var enemy in Utilities.GetPlayers()
                        .Where(p => p.IsValid && !p.IsHLTV && p.TeamNum != player.TeamNum
                            && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && p.PlayerPawn.Value.AbsOrigin != null))
                    {
                        float d = Vectors.GetDistance(origin, enemy.PlayerPawn!.Value!.AbsOrigin!);
                        if (d <= radius + 40f)
                        {
                            CCSPlayerPawn ePawn = enemy.PlayerPawn.Value;
                            ePawn.Health -= damage;
                            Utilities.SetStateChanged(ePawn, "CBaseEntity", "m_iHealth");
                            if (ePawn.Health <= 0)
                            {
                                if (!enemy.IsBot && !enemy.IsHLTV)
                                    ePawn.CommitSuicide(false, true);
                                else
                                {
                                    try { ePawn.CommitSuicide(false, true); }
                                    catch
                                    {
                                        ePawn.Health = 0;
                                        Utilities.SetStateChanged(ePawn, "CBaseEntity", "m_iHealth");
                                    }
                                }
                            }
                        }
                    }
                }
                catch { DestroyBeams(player); }
            }
        }
    }
}
