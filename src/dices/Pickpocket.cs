using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Pickpocket : DiceBlueprint
    {
        public override string ClassName => "Pickpocket";
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Pickpocket(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (info.Attacker.Value == null || !info.Attacker.Value.IsValid) return HookResult.Continue;
            if (info.Attacker.Value.Index == entity.Index) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid || victim.InGameMoneyServices == null
                || attacker.InGameMoneyServices == null) return HookResult.Continue;

            if (victim.InGameMoneyServices.Account <= 0) return HookResult.Continue;

            float stealPercent = _config.Dices.Pickpocket.StealPercentMin +
                (float)_random.NextDouble() * (_config.Dices.Pickpocket.StealPercentMax - _config.Dices.Pickpocket.StealPercentMin);
            int stealAmount = (int)float.Round(victim.InGameMoneyServices.Account * stealPercent);
            if (stealAmount <= 0) return HookResult.Continue;

            victim.InGameMoneyServices.Account -= stealAmount;
            Utilities.SetStateChanged(victim, "CCSPlayerController", "m_pInGameMoneyServices");
            attacker.InGameMoneyServices.Account += stealAmount;
            Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pInGameMoneyServices");

            attacker.PrintToCenterAlert($"🤑 +${stealAmount}!");
            victim.PrintToCenterAlert($"😱 -${stealAmount}!");

            return HookResult.Continue;
        }
    }
}
