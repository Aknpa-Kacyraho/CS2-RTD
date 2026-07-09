using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Synced : DiceBlueprint
    {
        public override string ClassName => "Synced";
        public override List<string> Events => ["EventWeaponReload"];

        public Synced(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        public override void Reset() => _players.Clear();
        public override void Destroy() => Reset();

        public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || !_players.Contains(player))
                return HookResult.Continue;

            // Force all other alive players to reload their active weapon
            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p != player
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                CCSPlayerPawn pawn = p.PlayerPawn!.Value;
                if (pawn.WeaponServices?.ActiveWeapon?.Value is not CBasePlayerWeapon activeWeapon
                    || !activeWeapon.IsValid)
                    continue;

                if (activeWeapon.Clip1 > 0)
                {
                    activeWeapon.Clip1 = 0;
                    Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
                }
            }

            return HookResult.Continue;
        }
    }
}
