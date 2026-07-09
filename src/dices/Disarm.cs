using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Disarm : DiceBlueprint
    {
        public override string ClassName => "Disarm";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        public Disarm(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "PistolMaster");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "缴械大师", "缴械率翻倍");
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
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();

            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid || victim == attacker
                || victim.TeamNum == attacker.TeamNum)
                return HookResult.Continue;
            if (victim.PlayerPawn?.Value?.WeaponServices == null) return HookResult.Continue;

            // Drop active weapon
            CCSPlayerPawn victimPawn = victim.PlayerPawn.Value;
            var activeWeapon = victimPawn.WeaponServices.ActiveWeapon;
            if (activeWeapon?.Value != null && activeWeapon.Value.IsValid)
            {
                string weaponName = activeWeapon.Value.DesignerName ?? "";
                // Don't drop knife
                if (!weaponName.Contains("knife") && !weaponName.Contains("c4"))
                {
                    victim.DropActiveWeapon();
                    victim.PrintToCenterAlert("🔫 武器被打掉了!");
                }
            }

            return HookResult.Continue;
        }
    }
}
