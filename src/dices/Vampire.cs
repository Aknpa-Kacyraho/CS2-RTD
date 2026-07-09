using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Vampire : DiceBlueprint
    {
        public override string ClassName => "Vampire";
        public override List<string> Events => ["EventPlayerHurt"];
        public readonly Random _random = new();
        public readonly Dictionary<CCSPlayerController, float> _playerSpeed = [];
        private bool _comboActive;
        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];

        public Vampire(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            _originalMaxHealth[player] = pawn.MaxHealth;
            pawn.MaxHealth = 333;
            pawn.Health = Math.Min(pawn.Health, 333);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            _comboActive = DiceSynergy.HasPartner(player, "SoulEater");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "噬魂血族", "HP上限333！吸血翻倍+回复护甲！");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                if (_originalMaxHealth.TryGetValue(player, out int origMax))
                {
                    pawn.MaxHealth = origMax;
                    pawn.Health = Math.Min(pawn.Health, origMax);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    _originalMaxHealth.Remove(player);
                }
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Remove(p);
            _players.Clear();
            _originalMaxHealth.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || victim == null || !_players.Contains(attacker) || attacker.PlayerPawn?.Value == null)
                return HookResult.Continue;

            int heal = (int)float.Round(@event.DmgHealth);
            if (_comboActive) heal *= 2;

            CCSPlayerPawn? pawn = attacker.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;
            pawn.Health = Math.Min(pawn.Health + heal, 333);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            attacker.PrintToCenterAlert($"+{heal} HP!");
            return HookResult.Continue;
        }
    }
}
