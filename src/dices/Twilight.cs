using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Twilight : DiceBlueprint
    {
        public override string ClassName => "Twilight";
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private bool _swapped;

        public Twilight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            if (!_swapped)
            {
                _swapped = true;
                Server.NextFrame(() => SwapAllPlayers());
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset() { _players.Clear(); _swapped = false; }
        public override void Destroy() => Reset();

        private void SwapAllPlayers()
        {
            var alive = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.PlayerPawn.Value.AbsOrigin != null)
                .ToList();

            if (alive.Count < 2) return;

            var positions = alive.Select(p => (p, new Vector(p.PlayerPawn!.Value.AbsOrigin!.X, p.PlayerPawn.Value.AbsOrigin.Y, p.PlayerPawn.Value.AbsOrigin.Z))).ToList();
            var shuffled = positions.Select(p => p.Item2).ToList();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i].p.PlayerPawn!.Value.Teleport(shuffled[i], new QAngle(0, 0, 0), new Vector(0, 0, 0));
            }

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌅 黄昏之时！全员位置随机互换！");
        }
    }
}
