using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class BoneMaggot : DiceBlueprint
    {
        public override string ClassName => "BoneMaggot";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        // Dedup: prevent re-marking the same victim within the mark duration
        private readonly Dictionary<ulong, float> _markedVictims = [];

        public BoneMaggot(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _markedVictims.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();

            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid || victim == attacker
                || victim.TeamNum == attacker.TeamNum)
                return HookResult.Continue;
            if (victim.PlayerPawn?.Value == null || !victim.PlayerPawn.Value.IsValid) return HookResult.Continue;

            // Dedup: don't re-mark same victim within duration
            float now = (float)Server.CurrentTime;
            if (_markedVictims.TryGetValue(victim.SteamID, out float expire) && now < expire)
                return HookResult.Continue;

            float duration = _config.Dices.BoneMaggot.MarkDuration;
            _markedVictims[victim.SteamID] = now + duration;

            // Create green glow on victim (visible through walls via GlowType)
            CCSPlayerPawn victimPawn = victim.PlayerPawn.Value;
            var (glowProxy, glow) = GlowUtil.CreateGlow(victimPawn, Color.FromArgb(255, 50, 255, 50));
            if (glow != null)
            {
                glow.Glow.GlowType = 3;
                glow.Glow.GlowRange = 5000;
                glow.Glow.GlowRangeMin = 0;
            }

            // Spawn burst particle at victim position
            CParticleSystem? particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle != null)
            {
                particle.EffectName = "particles/critters/chicken/chicken_impact_burst_zombie.vpcf";
                particle.Teleport(victimPawn.AbsOrigin, new QAngle(), new Vector());
                particle.StartActive = true;
                particle.DispatchSpawn();
                new CounterStrikeSharp.API.Modules.Timers.Timer(2f, () =>
                {
                    if (particle != null && particle.IsValid) particle.Remove();
                });
            }

            victim.PrintToCenterAlert("🐛 你被标记了!");

            // Cleanup after duration
            new CounterStrikeSharp.API.Modules.Timers.Timer(duration, () =>
            {
                if (glowProxy != null && glowProxy.IsValid) glowProxy.Remove();
                if (glow != null && glow.IsValid) glow.Remove();
            });

            return HookResult.Continue;
        }
    }
}
