using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Heaven : DiceBlueprint
    {
        public override string ClassName => "Heaven";
        public override List<string> Listeners => ["OnTick", "OnPlayerButtonsChanged"];
        private bool _comboActive;

        private bool _active;
        private float _timescale;
        private float _nextStepTime;

        public Heaven(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "World") || DiceSynergy.HasPartner(player, "Izayoi");
            if (_comboActive)
            {
                var instance = RollTheDice.Instance;
                bool sameWorld = instance != null && instance.HasDiceActive(player, "World");
                bool sameIzayoi = instance != null && instance.HasDiceActive(player, "Izayoi");
                if (sameWorld || sameIzayoi)
                {
                    DiceSynergy.AnnounceCombo(player, "超越天堂", "天堂与世界/十六夜融合！获得超越天堂之力！");
                    var captured = player;
                    Server.NextFrame(() =>
                    {
                        if (instance != null && captured.IsValid)
                        {
                            if (DiceSynergy.HasPartner(captured, "World"))
                                instance.RemoveDiceFromPlayer(captured, "World");
                            if (DiceSynergy.HasPartner(captured, "Izayoi"))
                                instance.RemoveDiceFromPlayer(captured, "Izayoi");
                            instance.RemoveDiceFromPlayer(captured, "Heaven");
                            instance.ForceDiceForPlayer(captured, "BeyondHeaven");
                        }
                    });
                    return;
                }
                else
                    DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！天堂共鸣！");
            }
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🌌 按E键进入天堂！时间加速！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic) { _ = _players.Remove(player); ResetTimescale(); }
        public override void Reset() { _players.Clear(); ResetTimescale(); }
        public override void Destroy() => Reset();

        private void ResetTimescale() { _active = false; _timescale = 1f; Server.ExecuteCommand("host_timescale 1.0"); }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0 || _active) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;

            _active = true;
            _timescale = _config.Dices.Heaven.MinTimescale;
            _nextStepTime = (float)Server.CurrentTime + _config.Dices.Heaven.StepInterval;
            Server.ExecuteCommand($"host_timescale {_timescale:F1}");
            player.PrintToCenterAlert("🌌 天堂之门开启！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌌 {player.PlayerName} 打开了天堂之门！时间从{_config.Dices.Heaven.MinTimescale:F1}x加速！");
        }

        public void OnTick()
        {
            if (!_active) return;
            float now = (float)Server.CurrentTime;

            if (now >= _nextStepTime && _timescale < _config.Dices.Heaven.MaxTimescale)
            {
                _timescale = Math.Min(_timescale + _config.Dices.Heaven.Step, _config.Dices.Heaven.MaxTimescale);
                _nextStepTime = now + _config.Dices.Heaven.StepInterval;
                Server.ExecuteCommand($"host_timescale {_timescale:F1}");
                foreach (var p in _players.ToList())
                {
                    if (p?.IsValid == true)
                        p.PrintToCenterAlert($"⏱ {_timescale:F1}倍速");
                }
            }
        }
    }
}
