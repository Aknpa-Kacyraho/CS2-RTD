using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class LoanShark : DiceBlueprint
    {
        public override string ClassName => "LoanShark";
        public override List<string> Events => ["EventPlayerDeath"];

        private readonly HashSet<ulong> _hasKilledThisRound = [];

        public LoanShark(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            int loanAmount = _config.Dices.LoanShark.LoanAmount;
            player.InGameMoneyServices.Account = loanAmount;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;

                player.InGameMoneyServices.Account = 0;
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

                if (!_hasKilledThisRound.Contains(player.SteamID))
                {
                    player.PrintToCenterAlert("你没能还债！高利贷找上门了！");

                    if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid
                        && player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        if (!player.IsBot && !player.IsHLTV)
                            player.PlayerPawn.Value.CommitSuicide(false, true);
                        else
                        {
                            try { player.PlayerPawn.Value.CommitSuicide(false, true); }
                            catch
                            {
                                player.PlayerPawn.Value.Health = 0;
                                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                            }
                        }
                    }
                }
            }

            _players.Clear();
            _hasKilledThisRound.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || attacker == @event.Userid) return HookResult.Continue;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (player.SteamID == attacker.SteamID)
                {
                    _hasKilledThisRound.Add(attacker.SteamID);
                    break;
                }
            }
            return HookResult.Continue;
        }
    }
}
