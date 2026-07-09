using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Shield : DiceBlueprint
    {
        public override string ClassName => "Shield";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Shield(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }

            int armor = _random.Next(_config.Dices.Shield.ArmorMin, _config.Dices.Shield.ArmorMax + 1);
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            pawn.ArmorValue = Math.Min(pawn.ArmorValue + armor, 100);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            // Give item_assaultsuit for helmet
            if (_config.Dices.Shield.Helmet)
            {
                player.GiveNamedItem("item_assaultsuit");
            }

            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Evasion");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "钢铁壁垒", "钢铁壁垒联动生效！");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "armor", armor.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            info.Damage *= _comboActive ? 0.25f : 0.5f;
            return HookResult.Changed;
        }
    }
}
