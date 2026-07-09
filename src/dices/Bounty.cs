using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Bounty : DiceBlueprint
    {
        public override string ClassName => "Bounty";
        public override List<string> Events => [
            "EventPlayerDeath"
        ];

        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, ulong> _bountyTargets = [];
        private readonly List<Timer> _pendingTimers = [];
        private bool _comboActive;
        private static readonly string[] _rewardPool = [
            "Amber", "Adrenaline", "DeagleKing", "PoisonBlade", "ToxicSmoke"
        ];

        public Bounty(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Capitalist");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "赏金猎人", "赏金加倍！");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });

            var timer = new Timer(5f, () =>
            {
                if (player == null || !player.IsValid || !_players.Contains(player)) return;
                AssignBountyTarget(player);
            });
            _pendingTimers.Add(timer);
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _bountyTargets.Remove(player);
        }

        public override void Reset()
        {
            foreach (var t in _pendingTimers) t.Kill();
            _pendingTimers.Clear();
            _players.Clear();
            _bountyTargets.Clear();
        }

        public override void Destroy() => Reset();

        private void AssignBountyTarget(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var enemies = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p != player
                    && p.TeamNum != player.TeamNum
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            if (enemies.Count == 0) return;
            var target = enemies[_random.Next(enemies.Count)];
            _bountyTargets[player] = target.SteamID;
            player.PrintToChat($" {_localizer["command.prefix"].Value}🎯 赏金目标：{target.PlayerName}！击杀获得额外随机骰子！");
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid) return HookResult.Continue;

            if (attacker.InGameMoneyServices != null)
            {
                int money = _random.Next(_config.Dices.Bounty.MoneyMin, _config.Dices.Bounty.MoneyMax + 1);
                if (_comboActive) money *= 2;
                attacker.InGameMoneyServices.Account += money;
                Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pInGameMoneyServices");
                attacker.PrintToCenterAlert($"💰 赏金 +${money}!");
            }

            if (_bountyTargets.TryGetValue(attacker, out ulong targetSteamId) && targetSteamId == victim.SteamID)
            {
                string diceReward = _rewardPool[_random.Next(_rewardPool.Length)];
                string translatedName = _localizer[$"dice_{diceReward}_name"].Value;
                RollTheDice.Instance?.ForceDiceForPlayer(attacker, diceReward);
                attacker.PrintToChat($" {_localizer["command.prefix"].Value}🎯 击杀赏金目标！额外获得骰子：{translatedName}！");
                _bountyTargets.Remove(attacker);
            }

            return HookResult.Continue;
        }
    }
}
