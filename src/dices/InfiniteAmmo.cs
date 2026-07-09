using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class InfiniteAmmo : DiceBlueprint
    {
        public override string ClassName => "InfiniteAmmo";
        public override List<string> Events => [
            "EventWeaponFire",
            "EventWeaponReload"
        ];
        public readonly Random _random = new();

        public InfiniteAmmo(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || !_players.Contains(player))
                return HookResult.Continue;

            if (player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value is not CBasePlayerWeapon activeWeapon
                || !activeWeapon.IsValid)
                return HookResult.Continue;

            activeWeapon.Clip1 = activeWeapon.VData?.MaxClip1 ?? 30;
            return HookResult.Continue;
        }

        public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || !_players.Contains(player))
                return HookResult.Continue;

            return HookResult.Stop;
        }
    }
}
