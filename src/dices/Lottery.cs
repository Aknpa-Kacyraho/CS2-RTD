using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Lottery : DiceBlueprint
    {
        public override string ClassName => "Lottery";
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Lottery(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            // Give random money to ALL players
            foreach (var p in Utilities.GetPlayers())
            {
                if (p == null || !p.IsValid || p.IsHLTV) continue;
                if (p.InGameMoneyServices == null) continue;

                int amount = _random.Next(_config.Dices.Lottery.MoneyMin, _config.Dices.Lottery.MoneyMax + 1);
                if (amount == 0) continue;

                if (amount > 0)
                {
                    p.InGameMoneyServices.Account += amount;
                    p.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_Lottery_win"].Value.Replace("{amount}", amount.ToString())}");
                }
                else
                {
                    int newBalance = Math.Max(0, p.InGameMoneyServices.Account + amount);
                    p.InGameMoneyServices.Account = newBalance;
                    p.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_Lottery_lose"].Value.Replace("{amount}", Math.Abs(amount).ToString())}");
                }

                Utilities.SetStateChanged(p, "CCSPlayerController", "m_pInGameMoneyServices");
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Lottery_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }
    }
}
