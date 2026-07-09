using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Jammer : DiceBlueprint
    {
        public override string ClassName => "Jammer";
        public override List<string> Listeners => ["OnTick"];
        public readonly Random _random = new();

        private const uint HIDE_ALL_MASK = (1u << 0) | (1u << 1) | (1u << 2) | (1u << 3) |
                                           (1u << 4) | (1u << 5) | (1u << 6) | (1u << 7) |
                                           (1u << 8) | (1u << 9) | (1u << 10) | (1u << 11) |
                                           (1u << 12) | (1u << 13) | (1u << 14);
        // Exclude weapon selection (bit 2, value 4) so players can still switch weapons
        private const uint HIDE_ALLOW_WEAPON_SWITCH = HIDE_ALL_MASK & ~(1u << 2);

        private readonly Dictionary<CCSPlayerController, uint> _originalHUDs = [];
        private CCSPlayerController? _jammerPlayer;

        public Jammer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            _players.Add(player);
            _jammerPlayer = player;

            foreach (var p in Utilities.GetPlayers())
            {
                if (p?.PlayerPawn?.Value == null || !p.PlayerPawn.Value.IsValid) continue;
                if (p == player) continue; // Jammer owner is unaffected

                _originalHUDs[p] = p.PlayerPawn.Value.HideHUD;
                p.PlayerPawn.Value.HideHUD |= HIDE_ALLOW_WEAPON_SWITCH;
                Utilities.SetStateChanged(p.PlayerPawn.Value, "CBasePlayerPawn", "m_iHideHUD");
            }

            SpeedBonusManager.Register(player, "Jammer", _config.Dices.Jammer.SpeedMultiplier - 1.0f);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            RestoreAllHUDs();
            SpeedBonusManager.Unregister(player, "Jammer");
            _ = _players.Remove(player);
            _jammerPlayer = null;
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
                Remove(player);
            _players.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                float effective = SpeedBonusManager.GetEffective(player);
                player.PlayerPawn.Value.VelocityModifier = 1 + effective;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        private void RestoreAllHUDs()
        {
            foreach (var kv in _originalHUDs)
            {
                var p = kv.Key;
                if (p?.PlayerPawn?.Value == null || !p.PlayerPawn.Value.IsValid) continue;
                p.PlayerPawn.Value.HideHUD = kv.Value;
                Utilities.SetStateChanged(p.PlayerPawn.Value, "CBasePlayerPawn", "m_iHideHUD");
            }
            _originalHUDs.Clear();
        }
    }
}
