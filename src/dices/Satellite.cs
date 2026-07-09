using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Satellite : DiceBlueprint
    {
        public override string ClassName => "Satellite";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick"];
        public override List<string> Events => ["EventWeaponFire"];

        public Satellite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Drone");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "天网", "无人机伤害+50%！卫星浮空！");

            float gravity = _config.Dices.Satellite.Gravity;
            player.PlayerPawn.Value.GravityScale = gravity;
            player.PlayerPawn.Value.ActualGravityScale = gravity;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flGravityScale");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.ActualGravityScale = 1.0f;
                // Restore normal flags (remove FL_ONGROUND if we forced it)
                Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_fFlags",
                    (uint)player.PlayerPawn.Value.Flags & ~1u);
            }
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                if (p?.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.ActualGravityScale = 1.0f;
                    Schema.SetSchemaValue(p.PlayerPawn.Value.Handle, "CBaseEntity", "m_fFlags",
                        (uint)p.PlayerPawn.Value.Flags & ~1u);
                }
            }
            _players.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float gravity = _config.Dices.Satellite.Gravity * (_comboActive ? 0.3f : 1f);
            foreach (var player in _players.ToList())
            {
                if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    // Force-set both gravity properties for reliability
                    pawn.GravityScale = gravity;
                    pawn.ActualGravityScale = gravity;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");

                    // Always force FL_ONGROUND flag for 100% accuracy at all times
                uint flags = (uint)pawn.Flags | 1u; // FL_ONGROUND = 1
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_fFlags", flags);
                }
            }
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !_players.Contains(player)) return HookResult.Continue;
            if (player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value is not CBasePlayerWeapon weapon) return HookResult.Continue;
            var wp = weapon.As<CCSWeaponBase>();
            wp.AccuracyPenalty = 0;
            wp.FlRecoilIndex = 0;
            return HookResult.Continue;
        }
    }
}
