using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using System.Drawing;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Deaf : DiceBlueprint
    {
        public override string ClassName => "Deaf";
        public override Dictionary<int, HookMode> UserMessages => new()
        {
            { 208, HookMode.Pre },
        };
        public override List<string> Listeners => [
            "OnPlayerButtonsChanged"
        ];

        private readonly Dictionary<CCSPlayerController, float> _cooldowns = [];
        private readonly Dictionary<CCSPlayerController, (CDynamicProp? Proxy, CDynamicProp? Glow, CCSPlayerController? Target)> _activeGlows = [];

        public Deaf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            _players.Add(player);
            _cooldowns[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🔇 聋！按E穿墙透视随机敌人4秒！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            RemoveGlowForPlayer(player);
            _ = _players.Remove(player);
            _ = _cooldowns.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                RemoveGlowForPlayer(p);
            _players.Clear();
            _cooldowns.Clear();
        }

        public override void Destroy() => Reset();

        private void RemoveGlowForPlayer(CCSPlayerController holder)
        {
            if (_activeGlows.TryGetValue(holder, out var glowInfo))
            {
                GlowUtil.RemoveGlow(glowInfo.Proxy, glowInfo.Glow);
                _ = _activeGlows.Remove(holder);
            }
        }

        public HookResult HookUserMessage208(UserMessage um)
        {
            if (_players.Count == 0) return HookResult.Continue;
            int sourceIndex = um.ReadInt("source_entity_index");
            foreach (var p in _players)
            {
                if (p?.PlayerPawn?.Value?.Index == sourceIndex)
                {
                    um.Recipients.Clear();
                    return HookResult.Stop;
                }
            }
            return HookResult.Continue;
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;

            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float now = (float)Server.CurrentTime;
            if (_cooldowns.TryGetValue(player, out float cd) && now < cd)
            {
                float remaining = cd - now;
                player.PrintToCenterAlert($"⏳ 冷却中... {remaining:F0}秒");
                return;
            }

            var enemies = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum != player.TeamNum
                    && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                    && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            if (enemies.Count == 0)
            {
                player.PrintToCenterAlert("❌ 没有存活的敌人！");
                return;
            }

            var target = enemies[Random.Shared.Next(enemies.Count)];
            if (target.PlayerPawn?.Value == null || !target.PlayerPawn.Value.IsValid) return;

            RemoveGlowForPlayer(player);

            var (proxy, glow) = GlowUtil.CreateGlow(target.PlayerPawn.Value, Color.Red);
            if (proxy == null || glow == null)
            {
                player.PrintToCenterAlert("❌ 透视失败！");
                return;
            }

            float duration = _config.Dices.Deaf.WallhackDuration;
            _activeGlows[player] = (proxy, glow, target);
            _cooldowns[player] = now + _config.Dices.Deaf.Cooldown;

            player.PrintToCenterAlert($"👁 透视 {target.PlayerName}！持续{duration}秒");

            CCSPlayerController captured = player;
            new CounterStrikeSharp.API.Modules.Timers.Timer(duration, () =>
            {
                if (_activeGlows.TryGetValue(captured, out var info))
                {
                    GlowUtil.RemoveGlow(info.Proxy, info.Glow);
                    _ = _activeGlows.Remove(captured);
                }
            });
        }
    }
}
