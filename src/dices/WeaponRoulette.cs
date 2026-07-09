using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class WeaponRoulette : DiceBlueprint
    {
        public override string ClassName => "WeaponRoulette";
        public override List<string> Events => [
            "EventWeaponFire"
        ];
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        private static readonly string[] _weaponList = [
            "weapon_ak47", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_aug", "weapon_sg556",
            "weapon_galilar", "weapon_famas", "weapon_awp", "weapon_ssg08", "weapon_scar20",
            "weapon_g3sg1", "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff",
            "weapon_m249", "weapon_negev", "weapon_mp9", "weapon_mac10", "weapon_mp7",
            "weapon_mp5sd", "weapon_ump45", "weapon_p90", "weapon_bizon", "weapon_deagle",
            "weapon_elite", "weapon_fiveseven", "weapon_glock", "weapon_hkp2000", "weapon_p250",
            "weapon_tec9", "weapon_usp_silencer", "weapon_cz75a", "weapon_revolver"
        ];

        public WeaponRoulette(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || !_players.Contains(player)
                || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return HookResult.Continue;

            Server.NextFrame(() =>
            {
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    return;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;

                // Save armor before RemoveWeapons (it strips armor in CS2)
                int savedArmor = pawn.ArmorValue;
                bool hasHelmet = pawn.ItemServices != null
                    && new CCSPlayer_ItemServices(pawn.ItemServices.Handle).HasHelmet;

                player.RemoveWeapons();

                string weapon = _weaponList[_random.Next(_weaponList.Length)];
                player.GiveNamedItem(weapon);
                player.GiveNamedItem("weapon_knife");

                // Restore armor after weapons
                pawn.ArmorValue = savedArmor;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                if (hasHelmet && pawn.ItemServices != null)
                    new CCSPlayer_ItemServices(pawn.ItemServices.Handle) { HasHelmet = true };
            });

            return HookResult.Continue;
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            info.Damage *= 1.3f;
            return HookResult.Changed;
        }
    }
}
