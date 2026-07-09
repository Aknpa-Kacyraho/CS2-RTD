using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Izayoi : DiceBlueprint
    {
        public override string ClassName => "Izayoi";
        public override List<string> Listeners => ["OnTick"];
        private bool _comboActive;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, float> _nextTriggerTime = [];
        // Multiple timescale options
        private static readonly float[] Timescales = [0.2f, 0.3f, 0.4f, 0.5f, 1.2f, 1.5f];

        public Izayoi(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Heaven");
            if (_comboActive)
            {
                var instance = RollTheDice.Instance;
                if (instance != null && instance.HasDiceActive(player, "Heaven"))
                {
                    DiceSynergy.AnnounceCombo(player, "超越天堂", "十六夜与天堂融合！获得超越天堂之力！");
                    var captured = player;
                    Server.NextFrame(() =>
                    {
                        if (instance != null && captured.IsValid)
                        {
                            instance.RemoveDiceFromPlayer(captured, "Izayoi");
                            instance.RemoveDiceFromPlayer(captured, "Heaven");
                            instance.ForceDiceForPlayer(captured, "BeyondHeaven");
                        }
                    });
                    return;
                }
                else
                    DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！十六夜与天堂共鸣！");
            }
            // Trigger the first effect almost immediately (~1.5s) so the player sees it
            // right away. Subsequent triggers use the configured interval.
            _nextTriggerTime[player] = (float)Server.CurrentTime + 1.5f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextTriggerTime.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _nextTriggerTime.Clear();
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;
            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid || !_nextTriggerTime.TryGetValue(player, out float next) || now < next)
                        continue;

                    float timescale = Timescales[_random.Next(Timescales.Length)];
                    float duration = _config.Dices.Izayoi.DurationSeconds;
                    string mode = timescale < 1.0f ? $"减速 ({timescale}x)" : $"加速 ({timescale}x)";

                    Server.ExecuteCommand($"host_timescale {timescale.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    Server.PrintToChatAll($"⏳ 时间被扰动了！{mode}");

                    new CounterStrikeSharp.API.Modules.Timers.Timer(duration, () =>
                    {
                        Server.ExecuteCommand("host_timescale 1.0");
                    });

                    _nextTriggerTime[player] = now + _config.Dices.Izayoi.IntervalSeconds;
                }
                catch { }
            }
        }
    }
}
