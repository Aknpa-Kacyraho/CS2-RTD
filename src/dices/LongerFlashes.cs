using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class LongerFlashes : DiceBlueprint
    {
        public override string ClassName => "LongerFlashes";
        public readonly Random _random = new();
        public override List<string> Events => [
            "EventPlayerBlind",
        ];

        public LongerFlashes(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult EventPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (player?.IsValid != true
                || attacker?.IsValid != true
                || !_players.Contains(attacker)
                || player.PlayerPawn.Value == null)
            {
                return HookResult.Continue;
            }
            float min = _config.Dices.LongerFlashes.MinBlinddurationFactor;
            float max = _config.Dices.LongerFlashes.MaxBlinddurationFactor;
            float blindDurationFactor = (float)((_random.NextDouble() * (max - min)) + min);
            @event.BlindDuration = (float)(@event.BlindDuration * blindDurationFactor);
            player.PlayerPawn.Value.FlashDuration = @event.BlindDuration;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawnBase", "m_flFlashDuration");
            player.PlayerPawn.Value.BlindUntilTime = Server.CurrentTime + player.PlayerPawn.Value.FlashDuration;
            player.PlayerPawn.Value.VelocityModifier = _config.Dices.LongerFlashes.SlowMultiplier;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");

            var captured = player;
            new CounterStrikeSharp.API.Modules.Timers.Timer(player.PlayerPawn.Value.FlashDuration, () =>
            {
                if (captured?.PlayerPawn?.Value != null && captured.PlayerPawn.Value.IsValid)
                {
                    captured.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(captured.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            });

            return HookResult.Continue;
        }
    }
}
