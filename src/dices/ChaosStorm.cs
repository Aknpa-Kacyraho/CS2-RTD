using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class ChaosStorm : DiceBlueprint
    {
        public override string ClassName => "ChaosStorm";
        public override List<string> Listeners => ["OnTick"];
        private float _nextSwapTime;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public ChaosStorm(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            if (_nextSwapTime == 0)
                _nextSwapTime = (float)Server.CurrentTime + _config.Dices.ChaosStorm.Interval;

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_ChaosStorm_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset() { _players.Clear(); _nextSwapTime = 0; }
        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0 || _nextSwapTime == 0) return;

            float now = (float)Server.CurrentTime;
            if (now < _nextSwapTime) return;

            float interval = _config.Dices.ChaosStorm.Interval;
            _nextSwapTime = now + interval;

            var alivePlayers = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.PlayerPawn.Value.AbsOrigin != null)
                .ToList();

            if (alivePlayers.Count < 2) return;

            var positions = alivePlayers
                .Select(p => new { Player = p, Pos = new Vector(p.PlayerPawn!.Value.AbsOrigin!.X, p.PlayerPawn.Value.AbsOrigin!.Y, p.PlayerPawn.Value.AbsOrigin!.Z) })
                .ToList();

            var shuffledPositions = positions.Select(p => p.Pos).ToList();
            for (int i = shuffledPositions.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (shuffledPositions[i], shuffledPositions[j]) = (shuffledPositions[j], shuffledPositions[i]);
            }

            for (int i = 0; i < positions.Count; i++)
            {
                var player = positions[i].Player;
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null)
                    continue;
                Vector targetPos = shuffledPositions[i];
                player.PlayerPawn.Value.Teleport(targetPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            }

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_ChaosStorm_swap"].Value}");
        }
    }
}
