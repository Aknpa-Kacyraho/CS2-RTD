using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class DiceBlueprint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
    {
        public readonly PluginConfig _globalConfig = GlobalConfig;
        public readonly MapConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly List<CCSPlayerController> _players = [];
        public virtual string Description { get; private set; } = "Unknown Dice";
        public virtual string ClassName => "DiceBlueprint";
        public virtual bool CanBeDrawn => true; // Set false for transformation-only dice

        // === v3.0: Generic n-special two-round distribution properties ===
        /// <summary>Relative probability weight for weighted random draws. Default 1.0 = uniform.</summary>
        public virtual float Weight => 1.0f;
        /// <summary>When true, this dice type triggers the two-round special distribution.</summary>
        public virtual bool IsSpecial => false;
        /// <summary>Probability per remaining player in round 2 for this special type to grant its reward.</summary>
        public virtual float SecondRoundProbability => 0.0f;
        /// <summary>If set, round 2 winners draw this dice class instead of the special itself. null = grant self.</summary>
        public virtual string? SecondRoundRewardId => null;

        // === Deprecated (kept for backward compat, no longer used by distribution engine) ===
        public virtual bool RequiresDrawSimulation => false; // DragonSoul, WolfKing etc trigger second draw round
        public virtual List<string> ExcludeFromReroll => []; // Dice to exclude from second-round re-roll when this special dice appears
        public virtual string? TeammateBonusDice => null; // Dice class name to grant unrolled teammates (null = no teammate bonus)
        public virtual float TeammateBonusChance => 0f; // Chance per unrolled teammate to receive TeammateBonusDice
        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual List<string> Precache => [];

        public virtual float GetCooldownRemaining(CCSPlayerController player) => 0f;

        public virtual void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public virtual void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public virtual void Reset()
        {
            _players.Clear();
        }

        public virtual void Destroy()
        {
            Reset();
        }

        public void NotifyPlayers(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            // Only notify the player who rolled — teammates see nothing
            if (!_localizer[$"dice_{diceName}_player"].ResourceNotFound
                && (_globalConfig.NotifyPlayerViaChatMsg || _globalConfig.NotifyPlayerViaCenterMsg))
            {
                string message = _localizer[$"dice_{diceName}_player"].Value;
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                if (_globalConfig.NotifyPlayerViaCenterMsg)
                {
                    player.PrintToCenter(message);
                }
                if (_globalConfig.NotifyPlayerViaChatMsg)
                {
                    player.PrintToChat(_localizer["command.prefix"].Value + message);
                }
                Description = message;
            }
        }

        public void NotifyStatus(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            // if player should get a message
            if (!_localizer[$"dice_{diceName}_status"].ResourceNotFound)
            {
                string message = _localizer[$"dice_{diceName}_status"].Value;
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                player.PrintToCenter(message);
                player.PrintToChat(_localizer["command.prefix"].Value + message);
                // update description if available
                Description = message;
            }
        }
    }
}