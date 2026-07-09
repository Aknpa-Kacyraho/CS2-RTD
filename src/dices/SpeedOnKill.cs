using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class SpeedOnKill : DiceBlueprint
    {
        public override string ClassName => "SpeedOnKill";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, Timer> _activeBoosts = [];

        private static readonly string[] _grenadePool = [
            "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_decoy"
        ];

        public SpeedOnKill(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

            _comboActive = DiceSynergy.HasPartner(player, "FrontlineBeast");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "猎杀本能", "猎杀时限翻倍");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (_activeBoosts.Remove(player, out Timer? timer))
            {
                timer?.Kill();
                if (player.PlayerPawn?.Value is { IsValid: true } pawn)
                {
                    pawn.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }

        public override void Reset()
        {
            foreach (var (player, timer) in _activeBoosts.ToList())
            {
                timer?.Kill();
                if (player?.PlayerPawn?.Value is { IsValid: true } pawn)
                {
                    pawn.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            _players.Clear();
            _activeBoosts.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null
                || !attacker.IsValid
                || victim == null
                || attacker == victim
                || !_players.Contains(attacker)
                || attacker.PlayerPawn?.Value == null
                || !attacker.PlayerPawn.Value.IsValid)
            {
                return HookResult.Continue;
            }

            float mult = _config.Dices.SpeedOnKill.SpeedMultiplierMin +
                (float)_random.NextDouble() * (_config.Dices.SpeedOnKill.SpeedMultiplierMax - _config.Dices.SpeedOnKill.SpeedMultiplierMin);
            float duration = _config.Dices.SpeedOnKill.DurationMin +
                (float)_random.NextDouble() * (_config.Dices.SpeedOnKill.DurationMax - _config.Dices.SpeedOnKill.DurationMin);
            if (_comboActive) duration *= 2f;

            CCSPlayerPawn pawn = attacker.PlayerPawn.Value;
            pawn.VelocityModifier = mult;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            attacker.PrintToCenterAlert($"⚡ 击杀加速 {((mult - 1f) * 100):F0}% {duration:F0}秒!");

            if (_activeBoosts.Remove(attacker, out Timer? oldTimer))
            {
                oldTimer?.Kill();
            }

            _activeBoosts[attacker] = new Timer(duration, () =>
            {
                if (attacker?.PlayerPawn?.Value is { IsValid: true } p)
                {
                    p.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(p, "CCSPlayerPawn", "m_flVelocityModifier");
                }
                _activeBoosts.Remove(attacker);
            });

            string grenadeName = _grenadePool[_random.Next(_grenadePool.Length)];
            attacker.GiveNamedItem(grenadeName);

            string displayName = grenadeName["weapon_".Length..];
            attacker.PrintToCenterAlert($"💣 +1 {displayName}!");

            return HookResult.Continue;
        }
    }
}
