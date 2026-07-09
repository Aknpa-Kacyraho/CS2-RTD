using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Rewind : DiceBlueprint
    {
        public override string ClassName => "Rewind";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerButtonsChanged"];
        public override List<string> Events => ["EventPlayerDeath"];

        // Track per-player: has used their rewind
        private readonly HashSet<CCSPlayerController> _used = [];
        // Track per-player: currently waiting for rewind countdown
        private readonly HashSet<CCSPlayerController> _pending = [];

        public Rewind(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Countdown");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "时空主宰", "时空主宰联动生效！");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("⏪ 回溯求源就绪！按E触发，15秒后全服回溯！被击杀则失效！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _used.Clear();
            _pending.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !_players.Contains(victim)) return HookResult.Continue;
            if (!_pending.Contains(victim)) return HookResult.Continue;

            // Player was counting down — cancel
            _pending.Remove(victim);
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{victim.PlayerName} 被杀，回溯已取消！");
            return HookResult.Continue;
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (_used.Contains(player)) return; // already used this round
            if (_pending.Contains(player)) return; // already counting down

            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            string? fileName = RollTheDice.GetRoundBackupFile();
            if (string.IsNullOrEmpty(fileName))
            {
                player.PrintToChat($" {_localizer["command.prefix"].Value}回溯失败：回合备份文件尚未就绪！");
                return;
            }

            // Mark as used and pending
            _used.Add(player);
            _pending.Add(player);

            // Broadcast to all players
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Rewind_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏪ {player.PlayerName} 发起了回溯！15秒后回滚到回合初！击杀{player.PlayerName}可阻止！");

            // Start 15s countdown
            string capturedFileName = fileName;
            int[] countdowns = { 10, 5, 3, 2, 1 };
            foreach (int cd in countdowns)
            {
                float delay = 15f - cd;
                new CounterStrikeSharp.API.Modules.Timers.Timer(delay, () =>
                {
                    if (!_pending.Contains(player)) return;
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏪ 回溯倒计时 {cd} 秒...");
                });
            }

            new CounterStrikeSharp.API.Modules.Timers.Timer(15f, () =>
            {
                if (!_pending.Contains(player)) return;
                _pending.Remove(player);

                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{player.PlayerName} 已死亡，回溯取消！");
                    return;
                }

                // Execute restore
                Server.ExecuteCommand($"mp_backup_restore_load_file {capturedFileName}");
            });
        }
    }
}
