using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class PistolMaster : DiceBlueprint
    {
        public override string ClassName => "PistolMaster";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];

        public PistolMaster(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Disarm");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "缴械大师", "手枪倍率2→3 缴械率翻倍");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            CCSPlayerPawn? attackerPawn = attacker.PlayerPawn.Value;
            if (attackerPawn?.WeaponServices?.ActiveWeapon?.Value == null)
                return HookResult.Continue;

            string weaponName = attackerPawn.WeaponServices.ActiveWeapon.Value.DesignerName.ToLower();
            if (weaponName == null) return HookResult.Continue;

            // Check if pistol: contains "pistol", "deagle", or "elite" but NOT "revolver"
            bool isPistol = weaponName.Contains("pistol")
                || weaponName.Contains("deagle")
                || weaponName.Contains("elite");

            bool isRevolver = weaponName.Contains("revolver");
            // "elite" is for dual elites, make sure "revolver" is excluded
            if (isRevolver && !weaponName.Contains("elite"))
                return HookResult.Continue;

            if (!isPistol || isRevolver)
                return HookResult.Continue;

            float multiplier = _config.Dices.PistolMaster.DamageMultiplier;
            if (_comboActive) multiplier += 1.0f; // 2x → 3x with Disarm combo
            info.Damage *= multiplier;
            return HookResult.Changed;
        }
    }
}
