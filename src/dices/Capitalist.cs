using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Capitalist : DiceBlueprint
    {
        public override string ClassName => "Capitalist";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerDeath"
        ];

        public Capitalist(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Bounty");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "赏金猎人", "赏金猎人联动生效！");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            int moneyPerDeath = _comboActive ? _config.Dices.Capitalist.MoneyPerDeath * 2 : _config.Dices.Capitalist.MoneyPerDeath;

            foreach (CCSPlayerController diceOwner in _players.ToList())
            {
                if (diceOwner == null
                    || !diceOwner.IsValid
                    || diceOwner.PlayerPawn?.Value == null
                    || !diceOwner.PlayerPawn.Value.IsValid
                    || diceOwner.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    continue;
                }

                diceOwner.InGameMoneyServices.Account += moneyPerDeath;
                Utilities.SetStateChanged(diceOwner, "CCSPlayerController", "m_pInGameMoneyServices");
            }

            return HookResult.Continue;
        }
    }
}
