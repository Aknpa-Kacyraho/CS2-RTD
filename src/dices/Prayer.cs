using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Prayer : DiceBlueprint
    {
        public override string ClassName => "Prayer";
        public override List<string> Listeners => ["OnTick"];
        private readonly Dictionary<CCSPlayerController, int> _successCount = [];
        private readonly Dictionary<CCSPlayerController, float> _nextPrayTime = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Prayer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _successCount[player] = 0;
            _nextPrayTime[player] = (float)Server.CurrentTime + _config.Dices.Prayer.PrayInterval;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player); _ = _successCount.Remove(player); _ = _nextPrayTime.Remove(player);
        }

        public override void Reset() { _players.Clear(); _successCount.Clear(); _nextPrayTime.Clear(); }
        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (!_nextPrayTime.TryGetValue(player, out float next) || now < next) continue;

                _nextPrayTime[player] = now + _config.Dices.Prayer.PrayInterval;
                bool success = _random.NextDouble() < _config.Dices.Prayer.SuccessChance;
                int count = _successCount.GetValueOrDefault(player, 0);

                var pawn = player.PlayerPawn.Value;

                if (success)
                {
                    count++;
                    _successCount[player] = count;
                    // +50 HP per successful prayer
                    pawn.Health = Math.Min(pawn.Health + 50, pawn.MaxHealth);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    player.PrintToCenterAlert($"🙏 祈愿 {count}/{_config.Dices.Prayer.SuccessNeeded} 次成功! +50HP");
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🙏 {player.PlayerName} 祈祷成功！+50HP ({count}/{_config.Dices.Prayer.SuccessNeeded})");

                    if (count >= _config.Dices.Prayer.SuccessNeeded)
                    {
                        foreach (var enemy in Utilities.GetPlayers()
                            .Where(e => e.IsValid && !e.IsHLTV && e.TeamNum != player.TeamNum
                                && e.PlayerPawn?.Value != null && e.PlayerPawn.Value.IsValid
                                && e.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                        {
                            if (!enemy.IsBot && !enemy.IsHLTV)
                                enemy.PlayerPawn!.Value.CommitSuicide(false, true);
                            else
                            {
                                try { enemy.PlayerPawn!.Value.CommitSuicide(false, true); }
                                catch { enemy.PlayerPawn!.Value.Health = 0; Utilities.SetStateChanged(enemy.PlayerPawn.Value, "CBaseEntity", "m_iHealth"); }
                            }
                        }
                        Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💀 {player.PlayerName} 三次祈愿成功！敌方全灭！");
                        _successCount[player] = 0;
                    }
                }
                else
                {
                    // -50 HP on failure (can die)
                    pawn.Health -= 50;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    if (pawn.Health <= 0)
                    {
                        try { pawn.CommitSuicide(false, true); }
                        catch { pawn.Health = 0; Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth"); }
                    }
                    player.PrintToCenterAlert($"🙏 祈愿失败... -50HP ({count}/{_config.Dices.Prayer.SuccessNeeded})");
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🙏 {player.PlayerName} 祈愿失败... -50HP ({count}/{_config.Dices.Prayer.SuccessNeeded})");
                }
            }
        }
    }
}
