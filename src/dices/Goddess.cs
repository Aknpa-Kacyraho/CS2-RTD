using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Goddess : DiceBlueprint
    {
        public override string ClassName => "Goddess";
        public override List<string> Events => [];
        public override List<string> Listeners => [];
        public static bool ActiveThisRound = false;
        public static readonly HashSet<ulong> BlessedPlayers = [];
        private bool _comboActive;

        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Goddess(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            ActiveThisRound = true;
            _comboActive = DiceSynergy.HasPartner(player, "God");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "神之共鸣", "上帝伤害翻倍+女神多祝福一人！");

            // Pick random alive teammates (not self)
            int count = _comboActive ? 3 : 2;
            List<CCSPlayerController> teammates = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum == player.TeamNum
                    && p != player
                    && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                    && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            // Shuffle and pick (2 normally, 3 with combo)
            for (int i = teammates.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (teammates[i], teammates[j]) = (teammates[j], teammates[i]);
            }

            var chosen = teammates.Take(count).ToList();
            foreach (var t in chosen)
            {
                BlessedPlayers.Add(t.SteamID);
            }

            string message = _localizer["dice_Goddess_player"].Value;
            foreach (CCSPlayerController entry in Utilities.GetPlayers()
                .Where(p => p.IsValid && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid))
            {
                entry.PrintToChat(_localizer["command.prefix"].Value + message);
            }

            // Announce blessed players
            if (chosen.Count > 0)
            {
                string names = string.Join(", ", chosen.Select(p => p.PlayerName));
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Goddess_blessed"].Value.Replace("{names}", names)}");
            }

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);

            // Only deactivate if no other Goddess owners exist
            // AND the removal is not due to dice replacement (NewDice)
            if (_players.Count == 0 && reason != DiceRemoveReason.NewDice)
            {
                ActiveThisRound = false;
            }
        }

        public override void Reset()
        {
            _players.Clear();
            ActiveThisRound = false;
            BlessedPlayers.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }
    }
}
