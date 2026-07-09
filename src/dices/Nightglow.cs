using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Nightglow : DiceBlueprint
    {
        public override string ClassName => "Nightglow";
        public override List<string> Listeners => ["OnTick"];

        private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _playerGlows = [];
        private float _lastFadeReapply;
        private const float FadeReapplyInterval = 3.0f;

        public Nightglow(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            ApplyDarkFade();
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💡 {player.PlayerName} 释放了夜光！地图完全黑暗，所有玩家发光！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (_players.Count == 0) ClearEffects();
        }

        public override void Reset() { ClearEffects(); _players.Clear(); }
        public override void Destroy() => Reset();

        private void ApplyDarkFade()
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV))
            {
                var fadeMsg = UserMessage.FromPartialName("Fade");
                fadeMsg.SetInt("duration", 5000);
                fadeMsg.SetInt("hold_time", 999999);
                fadeMsg.SetInt("flags", 0x0001 | 0x0010);
                fadeMsg.SetInt("color", 0 | (0 << 8) | (0 << 16) | (160 << 24));
                fadeMsg.Recipients.Add(p);
                fadeMsg.Send();
            }
        }

        private void ClearEffects()
        {
            foreach (var kv in _playerGlows.ToList())
            {
                GlowUtil.RemoveGlow(kv.Value.Item1, kv.Value.Item2);
            }
            _playerGlows.Clear();

            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid))
            {
                var fadeMsg = UserMessage.FromPartialName("Fade");
                fadeMsg.SetInt("duration", 100); fadeMsg.SetInt("hold_time", 0);
                fadeMsg.SetInt("flags", 0x0001 | 0x0010); fadeMsg.SetInt("color", 0);
                fadeMsg.Recipients.Add(p); fadeMsg.Send();
            }
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            float now = (float)Server.CurrentTime;

            if (now - _lastFadeReapply > FadeReapplyInterval)
            {
                _lastFadeReapply = now;
                ApplyDarkFade();
            }

            var allAlivePlayers = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            var activeIds = new HashSet<nint>();
            foreach (var p in allAlivePlayers)
            {
                activeIds.Add(p.PlayerPawn!.Value!.Handle);

                if (!_playerGlows.ContainsKey(p))
                {
                    Color glowColor = p.TeamNum == (int)CsTeam.Terrorist ? Color.OrangeRed : Color.DodgerBlue;
                    _playerGlows[p] = GlowUtil.CreateGlow(p.PlayerPawn.Value, glowColor);
                }
            }

            foreach (var kv in _playerGlows.ToList())
            {
                if (kv.Key.PlayerPawn?.Value == null
                    || !kv.Key.PlayerPawn.Value.IsValid
                    || kv.Key.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                    || !activeIds.Contains(kv.Key.PlayerPawn.Value.Handle))
                {
                    GlowUtil.RemoveGlow(kv.Value.Item1, kv.Value.Item2);
                    _playerGlows.Remove(kv.Key);
                }
            }
        }
    }
}
