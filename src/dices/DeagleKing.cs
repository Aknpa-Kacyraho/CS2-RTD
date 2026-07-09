using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DeagleKing : DiceBlueprint
    {
        public override string ClassName => "DeagleKing";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        public DeagleKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "SniperElite") || DiceSynergy.HasPartner(player, "DeadHand");
            if (DiceSynergy.HasPartner(player, "SniperElite"))
                DiceSynergy.AnnounceCombo(player, "精准猎杀", "精准猎杀联动生效！");
            if (DiceSynergy.HasPartner(player, "DeadHand"))
                DiceSynergy.AnnounceCombo(player, "致命一击", "开枪自伤减半+命中回血翻倍！");
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

            CCSPlayerPawn? attackerPawn = attacker.PlayerPawn?.Value;
            if (attackerPawn?.WeaponServices?.ActiveWeapon?.Value == null) return HookResult.Continue;

            string weaponName = attackerPawn.WeaponServices.ActiveWeapon.Value.DesignerName;
            if (!weaponName.Contains("deagle", StringComparison.OrdinalIgnoreCase))
                return HookResult.Continue;

            info.Damage *= _comboActive ? 5f : 3f;
            return HookResult.Changed;
        }
    }
}
