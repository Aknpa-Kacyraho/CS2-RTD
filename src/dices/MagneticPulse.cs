using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class MagneticPulse : DiceBlueprint
    {
        public override string ClassName => "MagneticPulse";
        private bool _comboActive;
        private bool _hasThorns;
        private bool _hasRepulsionField;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        // Damage tracking: victim SteamID → (attacker SteamID → damage in window)
        private readonly Dictionary<ulong, Dictionary<ulong, float>> _windowDamage = [];
        private readonly Dictionary<ulong, float> _windowStart = [];
        // Cooldown: victim SteamID → next allowed pulse time
        private readonly Dictionary<ulong, float> _cooldownEnd = [];
        // Slow tracking: enemy SteamID → slow end time
        private readonly Dictionary<ulong, float> _slowEnd = [];
        // Pending triggers (set in damage hook, processed in OnTick for visual+effect)
        private readonly HashSet<ulong> _pendingPulse = [];

        public MagneticPulse(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _hasThorns = DiceSynergy.HasPartner(player, "Thorns");
            _hasRepulsionField = DiceSynergy.HasPartner(player, "RepulsionField");
            _comboActive = _hasThorns || _hasRepulsionField;

            if (_hasThorns)
                DiceSynergy.AnnounceCombo(player, "磁力荆棘", "魔镜反弹+磁力脉冲缴械！双重重压！");
            if (_hasRepulsionField)
                DiceSynergy.AnnounceCombo(player, "禁区", "斥力场弹回投掷物+磁力脉冲缴械！完全封锁远程！");

            _windowDamage[player.SteamID] = [];
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            CleanupPlayer(player.SteamID);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                CleanupPlayer(p.SteamID);
            _players.Clear();
            _windowDamage.Clear();
            _windowStart.Clear();
            _cooldownEnd.Clear();
            _slowEnd.Clear();
            _pendingPulse.Clear();
        }

        public override void Destroy() => Reset();

        private void CleanupPlayer(ulong steamID)
        {
            _windowDamage.Remove(steamID);
            _windowStart.Remove(steamID);
            _cooldownEnd.Remove(steamID);
            _pendingPulse.Remove(steamID);
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || attacker == victim) return HookResult.Continue;
            if (attacker.TeamNum == victim.TeamNum) return HookResult.Continue;

            ulong vSid = victim.SteamID;
            ulong aSid = attacker.SteamID;
            float now = (float)Server.CurrentTime;

            // Cooldown check
            if (_cooldownEnd.TryGetValue(vSid, out float cdEnd) && now < cdEnd)
                return HookResult.Continue;

            float window = _comboActive ? _config.Dices.MagneticPulse.DamageWindow * 0.5f : _config.Dices.MagneticPulse.DamageWindow;

            // Reset damage window if expired
            if (!_windowStart.TryGetValue(vSid, out float wStart) || now - wStart > window)
            {
                _windowDamage[vSid] = [];
                _windowStart[vSid] = now;
            }

            // Same attacker within window → skip (prevents molotov spam)
            if (!_windowDamage.TryGetValue(vSid, out var attackerDmg))
            {
                attackerDmg = [];
                _windowDamage[vSid] = attackerDmg;
            }
            if (attackerDmg.ContainsKey(aSid))
                return HookResult.Continue;

            attackerDmg[aSid] = info.Damage;
            float total = 0f;
            foreach (var d in attackerDmg.Values) total += d;

            float threshold = _comboActive ? _config.Dices.MagneticPulse.DamageThreshold * 0.5f : _config.Dices.MagneticPulse.DamageThreshold;

            if (total >= threshold)
            {
                _pendingPulse.Add(vSid);
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            // Process pending pulses
            foreach (var sid in _pendingPulse.ToList())
            {
                _pendingPulse.Remove(sid);

                var player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && !p.IsHLTV && p.SteamID == sid);
                if (player?.PlayerPawn?.Value?.AbsOrigin == null) continue;

                // Set cooldown
                float cd = _comboActive ? _config.Dices.MagneticPulse.Cooldown * 0.5f : _config.Dices.MagneticPulse.Cooldown;
                _cooldownEnd[sid] = now + cd;

                // Clear damage window
                _windowDamage.Remove(sid);
                _windowStart.Remove(sid);

                Vector origin = player.PlayerPawn.Value.AbsOrigin;
                float radius = _config.Dices.MagneticPulse.Radius;
                float slowAmount = _hasRepulsionField ? _config.Dices.MagneticPulse.SlowAmount * 1.5f : _config.Dices.MagneticPulse.SlowAmount;
                float slowDuration = _hasThorns ? _config.Dices.MagneticPulse.SlowDuration * 1.5f : _config.Dices.MagneticPulse.SlowDuration;

                // Spawn blue EMP visual ring
                SpawnPulseVisual(origin);

                // Play EMP sound
                player.EmitSound("CSGO_EMP.Impact");

                player.PrintToCenterAlert("⚡ 磁力脉冲释放！");

                int disarmed = 0;
                foreach (var enemy in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV && p != player
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                        && p.PlayerPawn.Value.AbsOrigin != null))
                {
                    float dx = origin.X - enemy.PlayerPawn!.Value!.AbsOrigin!.X;
                    float dy = origin.Y - enemy.PlayerPawn!.Value!.AbsOrigin!.Y;
                    float dz = origin.Z - enemy.PlayerPawn!.Value!.AbsOrigin!.Z;
                    float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (dist > radius) continue;

                    // Disarm: drop active weapon (not knife/C4)
                    var wpSvcs = enemy.PlayerPawn!.Value!.WeaponServices;
                    var activeWeapon = wpSvcs?.ActiveWeapon;
                    if (activeWeapon?.Value != null && activeWeapon.Value.IsValid)
                    {
                        string wpName = activeWeapon.Value.DesignerName ?? "";
                        if (!wpName.Contains("knife") && !wpName.Contains("bayonet") && !wpName.Contains("c4"))
                        {
                            enemy.DropActiveWeapon();
                            disarmed++;
                        }
                    }

                    // Apply slow
                    SpeedBonusManager.Register(enemy, "MagneticPulse", -slowAmount);
                    _slowEnd[enemy.SteamID] = now + slowDuration;

                    enemy.PrintToCenterAlert($"⚡ 磁力脉冲！缴械+减速{(int)(slowAmount * 100)}% {slowDuration:F0}秒！");
                }

                if (disarmed > 0)
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⚡ {player.PlayerName} 释放磁力脉冲！缴械{disarmed}名敌人！");
            }

            // Maintain speed slow on enemies
            var toRemove = new List<ulong>();
            foreach (var kv in _slowEnd.ToList())
            {
                if (now >= kv.Value)
                {
                    var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == kv.Key);
                    if (enemy != null)
                        SpeedBonusManager.Unregister(enemy, "MagneticPulse");
                    toRemove.Add(kv.Key);
                    continue;
                }

                // Maintain slow (engine may reset)
                var ep = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && !p.IsHLTV
                    && p.SteamID == kv.Key
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE);
                if (ep != null)
                {
                    float slowAmount = _hasRepulsionField ? _config.Dices.MagneticPulse.SlowAmount * 1.5f : _config.Dices.MagneticPulse.SlowAmount;
                    float effective = SpeedBonusManager.GetEffective(ep, -slowAmount);
                    ep.PlayerPawn!.Value!.VelocityModifier = 1 + effective;
                    Utilities.SetStateChanged(ep.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            foreach (var k in toRemove) _slowEnd.Remove(k);
        }

        private void SpawnPulseVisual(Vector pos)
        {
            // Blue EMP particle
            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle != null)
            {
                particle.EffectName = "particles/ui/ui_experience_award_innerpoint.vpcf";
                particle.StartActive = true;
                particle.Teleport(pos, new QAngle(), new Vector());
                particle.DispatchSpawn();

                // Auto-remove after 1.5s
                var captured = particle;
                _ = new CounterStrikeSharp.API.Modules.Timers.Timer(1.5f, () =>
                {
                    if (captured != null && captured.IsValid) captured.Remove();
                });
            }

            // Blue ring beam
            int segments = 12;
            float ringRadius = 60f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * MathF.PI / segments;
                float nextAngle = (i + 1) * 2f * MathF.PI / segments;
                float x1 = pos.X + ringRadius * MathF.Cos(angle);
                float y1 = pos.Y + ringRadius * MathF.Sin(angle);
                float x2 = pos.X + ringRadius * MathF.Cos(nextAngle);
                float y2 = pos.Y + ringRadius * MathF.Sin(nextAngle);

                var beam = Utilities.CreateEntityByName<CBeam>("beam");
                if (beam == null) continue;
                beam.Render = Color.FromArgb(200, 30, 144, 255);
                beam.Width = 3f;
                beam.Teleport(new Vector(x1, y1, pos.Z + 30f), new QAngle(), new Vector());
                beam.EndPos.X = x2;
                beam.EndPos.Y = y2;
                beam.EndPos.Z = pos.Z + 30f;
                beam.DispatchSpawn();

                var capturedBeam = beam;
                _ = new CounterStrikeSharp.API.Modules.Timers.Timer(1f, () =>
                {
                    if (capturedBeam != null && capturedBeam.IsValid) capturedBeam.Remove();
                });
            }
        }
    }
}
