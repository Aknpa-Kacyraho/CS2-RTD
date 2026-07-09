using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class World : DiceBlueprint
    {
        public override string ClassName => "World";
        private bool _comboActive;
        /// <summary>
        /// Static dictionary for core system to check and grant extra dice rolls.
        /// The RollTheDice.cs core logic reads this and rolls additional dice.
        /// </summary>
        public static Dictionary<ulong, int> PendingExtraRolls = [];

        public World(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            PendingExtraRolls[player.SteamID] = _config.Dices.World.ExtraDiceCount;
            _comboActive = DiceSynergy.HasPartner(player, "Heaven");
            if (_comboActive)
            {
                var instance = RollTheDice.Instance;
                if (instance != null && instance.HasDiceActive(player, "Heaven"))
                {
                    DiceSynergy.AnnounceCombo(player, "超越天堂", "世界与天堂融合！获得超越天堂之力！");
                    var captured = player;
                    Server.NextFrame(() =>
                    {
                        if (instance != null && captured.IsValid)
                        {
                            instance.RemoveDiceFromPlayer(captured, "World");
                            instance.RemoveDiceFromPlayer(captured, "Heaven");
                            instance.ForceDiceForPlayer(captured, "BeyondHeaven");
                        }
                    });
                    return;
                }
                else
                    DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！世界与天堂共鸣！");
            }

            if (DiceSynergy.HasPartner(player, "Bugle"))
                DiceSynergy.AnnounceCombo(player, "天启", "团队联动！冲锋号+世界共鸣！");

            // Broadcast to all players
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_World_broadcast"].Value.Replace("{playerName}", player.PlayerName).Replace("{count}", _config.Dices.World.ExtraDiceCount.ToString())}");

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "extraCount", _config.Dices.World.ExtraDiceCount.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = PendingExtraRolls.Remove(player.SteamID);
        }

        public override void Reset()
        {
            _players.Clear();
            PendingExtraRolls.Clear();
        }
    }
}
