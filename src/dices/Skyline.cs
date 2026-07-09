using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Skyline : DiceBlueprint
    {
        public override string ClassName => "Skyline";
        public override List<string> Listeners => ["OnTick", "OnPlayerButtonsChanged"];

        public override float GetCooldownRemaining(CCSPlayerController player)
            => _cooldowns.TryGetValue(player, out float cd) ? Math.Max(0, cd - (float)Server.CurrentTime) : 0f;

        private readonly Dictionary<CCSPlayerController, float> _cooldowns = [];
        private readonly Dictionary<CCSPlayerController, float> _flightEndTime = [];

        public Skyline(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _cooldowns[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("☁ 天际就绪！按E飞行3秒！(冷却30秒)");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            EndFlight(player);
            _ = _players.Remove(player);
            _ = _cooldowns.Remove(player);
            _ = _flightEndTime.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) EndFlight(p);
            _players.Clear(); _cooldowns.Clear(); _flightEndTime.Clear();
        }

        public override void Destroy() => Reset();

        private void EndFlight(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return; // E key
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float now = (float)Server.CurrentTime;
            if (_cooldowns.TryGetValue(player, out float cd) && now < cd)
            {
                player.PrintToCenterAlert($"☁ 冷却中... {cd - now:F0}秒");
                return;
            }

            float dur = _config.Dices.Skyline.FlightDuration;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NOCLIP);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");

            _flightEndTime[player] = now + dur;
            _cooldowns[player] = now + _config.Dices.Skyline.Cooldown;

            player.PrintToCenterAlert($"☁ 飞行中！{dur}秒");
        }

        public void OnTick()
        {
            if (_flightEndTime.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var kv in _flightEndTime.ToList())
            {
                var player = kv.Key;
                if (now >= kv.Value)
                {
                    EndFlight(player);
                    _ = _flightEndTime.Remove(player);
                    player?.PrintToCenterAlert("☁ 飞行结束!");
                    continue;
                }

                // Maintain noclip during flight (engine may reset)
                if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.MoveType != MoveType_t.MOVETYPE_NOCLIP)
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NOCLIP);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                    }
                }
            }
        }
    }
}
