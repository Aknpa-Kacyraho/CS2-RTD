using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Universe : DiceBlueprint
    {
        public override string ClassName => "Universe";
        public override List<string> Events => ["EventPlayerDeath"];

        // Static: survive round restores (mp_backup_restore_load_file resets instance state)
        public static readonly Dictionary<ulong, int> RestoresLeft = [];
        /// <summary>Players whose restore just triggered — re-give Universe after OnRoundStart.</summary>
        public static readonly HashSet<ulong> PendingRestore = [];

        public Universe(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            int max = _config.Dices.Universe.MaxRestores;

            // Only init on FIRST add (not after restore re-adds)
            if (!RestoresLeft.ContainsKey(player.SteamID))
            {
                RestoresLeft[player.SteamID] = max;
                player.PrintToCenterAlert($"🌌 宇宙之力！死亡时自动回溯，剩余{max}次");
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName }, { "max", max.ToString() } });
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
            RestoresLeft.Clear();
            PendingRestore.Clear();
            _players.Clear();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !_players.Contains(victim)) return HookResult.Continue;
            if (!RestoresLeft.TryGetValue(victim.SteamID, out int left) || left <= 0) return HookResult.Continue;

            // Use the global round backup created by OnRoundStart
            string? fileName = RollTheDice.GetRoundBackupFile();
            if (string.IsNullOrEmpty(fileName)) return HookResult.Continue;

            // Decrement BEFORE restore (restore re-triggers Add which checks RestoresLeft)
            int newLeft = left - 1;
            RestoresLeft[victim.SteamID] = newLeft;

            // Mark player so OnRoundStart re-gives them Universe after restore
            PendingRestore.Add(victim.SteamID);

            string victimName = victim.PlayerName;

            // Notify before restore
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Universe_trigger"].Value.Replace("{playerName}", victimName).Replace("{left}", newLeft.ToString())}");

            // Restore the round — this will re-trigger OnRoundStart and re-add all dice
            Server.NextFrame(() =>
            {
                Server.ExecuteCommand($"mp_backup_restore_load_file {fileName}");
            });

            return HookResult.Continue;
        }
    }
}
