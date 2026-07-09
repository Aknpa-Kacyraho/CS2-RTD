using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class SniperElite : DiceBlueprint
    {
        public override string ClassName => "SniperElite";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        private static readonly HashSet<string> SniperWeapons = [
            "weapon_awp",
            "weapon_ssg08",
            "weapon_scar20",
            "weapon_g3sg1"
        ];

        public SniperElite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "DeagleKing");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "精准猎杀", "精准猎杀联动生效！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            var pawn = attacker.PlayerPawn?.Value;
            if (pawn?.WeaponServices?.ActiveWeapon?.Value == null)
                return HookResult.Continue;

            string weaponName = pawn.WeaponServices.ActiveWeapon.Value.DesignerName;
            if (string.IsNullOrEmpty(weaponName) || !SniperWeapons.Contains(weaponName))
                return HookResult.Continue;

            info.Damage *= _comboActive ? _config.Dices.SniperElite.DamageMultiplier * 1.5f : _config.Dices.SniperElite.DamageMultiplier;
            return HookResult.Changed;
        }
    }
}
