using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Reincarnation : DiceBlueprint
    {
        public override string ClassName => "Reincarnation";
        public override List<string> Events => ["EventPlayerDeath"];

        // Static: carries over between rounds. SteamID -> pending extra dice count for next round
        public static readonly Dictionary<ulong, int> PendingExtraDice = [];

        public Reincarnation(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });

            int pending = PendingExtraDice.TryGetValue(player.SteamID, out int p) ? p : 0;
            if (pending > 0)
                player.PrintToCenterAlert($"🔄 轮回！下回合获得{pending}个额外骰子！");
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
            PendingExtraDice.Clear();
            _players.Clear();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !_players.Contains(victim)) return HookResult.Continue;

            int max = _config.Dices.Reincarnation.MaxStacks;
            int current = PendingExtraDice.TryGetValue(victim.SteamID, out int c) ? c : 0;
            if (current >= max) return HookResult.Continue;

            current++;
            PendingExtraDice[victim.SteamID] = current;
            victim.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_Reincarnation_progress"].Value.Replace("{count}", current.ToString())}");

            return HookResult.Continue;
        }
    }
}
