using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Trickster : DiceBlueprint
    {
        public override string ClassName => "Trickster";
        public override List<string> Events => ["EventPlayerDeath"];
        private bool _revealed;

        private static readonly string[] FakeNames = ["命悬一线", "疾风步", "千钧", "轻功", "铁腕", "涅槃"];
        public static Dictionary<ulong, string> PendingFakeNames = [];

        public Trickster(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _revealed = false;

            string fakeName = FakeNames[Random.Shared.Next(FakeNames.Length)];
            PendingFakeNames[player.SteamID] = fakeName;

            string fakeDesc = _localizer["dice_Trickster_fake"].Value.Replace("{fakeName}", fakeName);
            player.PrintToChat(_localizer["command.prefix"].Value + fakeDesc);
            player.PrintToCenterAlert($"🎭 你获得了 {fakeName}！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            PendingFakeNames.Remove(player.SteamID);
        }

        public override void Reset() { _players.Clear(); _revealed = false; PendingFakeNames.Clear(); }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (attacker == @event.Userid) return HookResult.Continue;
            if (_revealed) return HookResult.Continue;

            _revealed = true;
            PendingFakeNames.Remove(attacker.SteamID);

            attacker.PrintToCenterAlert($"🎭 诡术揭晓！露出了真正的面目...");
            attacker.PrintToChat(_localizer["command.prefix"].Value + _localizer["dice_Trickster_reveal"].Value);

            RollTheDice.Instance?.ForceExtraDiceForPlayer(attacker);

            return HookResult.Continue;
        }
    }
}
