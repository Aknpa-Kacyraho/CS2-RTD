using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Prophet : DiceBlueprint
    {
        public override string ClassName => "Prophet";
        public override List<string> Listeners => ["OnTick"];

        private float _lastRevealTime;

        public Prophet(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
        { _ = _players.Remove(player); }

        public override void Reset() { _players.Clear(); _lastRevealTime = 0; }
        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;
            if (now - _lastRevealTime < 5f) return;
            _lastRevealTime = now;

            foreach (var prophet in _players.ToList())
            {
                if (prophet == null || !prophet.IsValid) continue;

                var enemies = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV && p.TeamNum != prophet.TeamNum
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .ToList();

                var instance = RollTheDice.Instance;
                if (instance == null) continue;

                var enemyInfo = new List<string>();
                foreach (var enemy in enemies)
                {
                    var dices = instance.GetAllDiceForPlayer(enemy);
                    if (dices.Count > 0)
                    {
                        string diceNames = string.Join("+", dices.Select(d =>
                        {
                            string trans = _localizer.GetString($"dice_{d}_name", d);
                            if (d == "Trickster") return trans + "(真实)";
                            return trans;
                        }));
                        enemyInfo.Add($"{enemy.PlayerName}[{diceNames}]");
                    }
                }

                if (enemyInfo.Count > 0)
                    prophet.PrintToCenterAlert($"🔮 敌人骰子：{string.Join(" | ", enemyInfo)}");
            }
        }
    }
}
