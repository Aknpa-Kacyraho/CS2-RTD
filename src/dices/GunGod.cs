using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class GunGod : DiceBlueprint
    {
        public override string ClassName => "GunGod";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        public GunGod(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        public override void Reset() { _players.Clear(); }
        public override void Destroy() => Reset();

        private static readonly HashSet<string> _grenadeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile",
            "molotov_projectile", "incendiarygrenade_projectile", "decoy_projectile"
        };

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0 || info.Damage <= 0) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            // Immune to throwables (check Inflictor DesignerName — DMG_BLAST is unreliable in CS2)
            string? inflictorName = info.Inflictor?.Value?.DesignerName;
            if (inflictorName != null && _grenadeTypes.Contains(inflictorName))
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            // Immune to molotov/incendiary burn ticks
            if ((info.BitsDamageType & DamageTypes_t.DMG_BURN) != 0)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            // Immune to knife
            if ((info.BitsDamageType & DamageTypes_t.DMG_SLASH) != 0)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            // 66% damage reduction for everything else
            float reduction = _config.Dices.GunGod.DamageReduction;
            info.Damage *= (1.0f - reduction);

            return HookResult.Changed;
        }
    }
}
