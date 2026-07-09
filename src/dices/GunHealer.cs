using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class GunHealer : DiceBlueprint
    {
        public override string ClassName => "GunHealer";
        public override List<string> Events => [
            "EventWeaponFire"
        ];

        public GunHealer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_players.Contains(player)
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return HookResult.Continue;
            }

            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            int healPerShot = _config.Dices.GunHealer.HealPerShot;
            pawn.Health = Math.Min(pawn.Health + healPerShot, pawn.MaxHealth);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            return HookResult.Continue;
        }
    }
}
