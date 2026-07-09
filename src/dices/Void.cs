using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Void : DiceBlueprint
    {
        public override string ClassName => "Void";
        public override List<string> Listeners => ["OnTick", "OnPlayerButtonsChanged", "OnPlayerTakeDamagePre"];

        public override float GetCooldownRemaining(CCSPlayerController player)
            => _cooldownEnd.TryGetValue(player, out float cd) ? Math.Max(0, cd - (float)Server.CurrentTime) : 0f;
        public override List<string> Events => ["EventWeaponFire"];

        private readonly Dictionary<CCSPlayerController, float> _voidEndTime = [];
        private readonly Dictionary<CCSPlayerController, float> _cooldownEnd = [];
        private readonly Dictionary<CCSPlayerController, int> _lastVoidCountdown = [];

        public Void(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _voidEndTime[player] = 0f;
            _cooldownEnd[player] = 0f;
            _lastVoidCountdown[player] = -1;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🌑 按E键遁入虚无！无敌+飞行+隐身5s！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DeactivateVoid(player);
            _ = _players.Remove(player);
            _ = _voidEndTime.Remove(player);
            _ = _cooldownEnd.Remove(player);
            _ = _lastVoidCountdown.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                DeactivateVoid(p);
            _players.Clear();
            _voidEndTime.Clear();
            _cooldownEnd.Clear();
            _lastVoidCountdown.Clear();
        }

        public override void Destroy() => Reset();

        private void ActivateVoid(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value is not CCSPlayerPawn pawn || !pawn.IsValid) return;
            float now = (float)Server.CurrentTime;
            _voidEndTime[player] = now + _config.Dices.Void.Duration;
            _cooldownEnd[player] = now + _config.Dices.Void.Cooldown;

            pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NOCLIP);

            pawn.Render = Color.FromArgb(20, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            player.PrintToCenterAlert("🌑 遁入虚无！5s无敌+飞行+隐身！");
        }

        private void DeactivateVoid(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            if (player.PlayerPawn?.Value is not CCSPlayerPawn pawn || !pawn.IsValid) return;
            if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_WALK);

            pawn.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float now = (float)Server.CurrentTime;
            if (_cooldownEnd.TryGetValue(player, out float cd) && now < cd) return;
            if (_voidEndTime.TryGetValue(player, out float active) && now < active) return;

            ActivateVoid(player);
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;

                if (_voidEndTime.TryGetValue(player, out float endTime) && now >= endTime && endTime > 0)
                {
                    _voidEndTime[player] = 0f;
                    DeactivateVoid(player);
                    player.PrintToCenterAlert("☀️ 虚无消散！回到现实！");
                }

                if (_voidEndTime.TryGetValue(player, out float active2) && now < active2 && active2 > 0)
                {
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.MoveType != MoveType_t.MOVETYPE_NOCLIP)
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NOCLIP);
                    }

                    int remaining = (int)Math.Ceiling(active2 - now);
                    int lastShown = _lastVoidCountdown.GetValueOrDefault(player, -1);
                    if (remaining > 0 && remaining != lastShown)
                    {
                        _lastVoidCountdown[player] = remaining;
                        player.PrintToCenterAlert($"🌑 虚无！{remaining}秒剩余");
                    }
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            float now = (float)Server.CurrentTime;

            // Block damage TO void player
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim != null && victim.IsValid && _players.Contains(victim))
            {
                if (_voidEndTime.TryGetValue(victim, out float victimEnd) && now < victimEnd)
                {
                    info.Damage = 0;
                    return HookResult.Changed;
                }
            }

            // Block damage FROM void player (can't shoot during void)
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker != null && attacker.IsValid && _players.Contains(attacker))
            {
                if (_voidEndTime.TryGetValue(attacker, out float attackerEnd) && now < attackerEnd)
                {
                    info.Damage = 0;
                    return HookResult.Changed;
                }
            }

            return HookResult.Continue;
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || !_players.Contains(player))
                return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            if (_voidEndTime.TryGetValue(player, out float end) && now < end)
                return HookResult.Stop;

            return HookResult.Continue;
        }
    }
}
