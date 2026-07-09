using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Cutter : DiceBlueprint
    {
        public override string ClassName => "Cutter";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerHurt"
        ];
        public override List<string> Listeners => ["OnTick"];

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "SwordSaint");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "剑刃风暴", "刺客信条+剑仙！刀剑合璧，移速×1.5！");
            SpeedBonusManager.Register(player, "Cutter", _config.Dices.Cutter.SpeedMultiplier - 1.0f);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            SpeedBonusManager.Unregister(player, "Cutter");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                SpeedBonusManager.Unregister(p, "Cutter");
                _players.Remove(p);
            }
            _players.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                float effective = SpeedBonusManager.GetEffective(player);
                float speed = 1 + effective;
                if (_comboActive) speed *= 1.5f; // SwordSaint combo: 1.5x speed
                player.PlayerPawn.Value.VelocityModifier = speed;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        private readonly HashSet<string> _knifes = new(StringComparer.OrdinalIgnoreCase)
        {
            "knife",
            CsItem.Knife.ToString(),
            CsItem.KnifeT.ToString(),
            CsItem.KnifeCT.ToString(),
            CsItem.DefaultKnifeT.ToString(),
            CsItem.DefaultKnifeCT.ToString()
        };

        public Cutter(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }


        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (player?.IsValid != true
                || attacker?.IsValid != true
                || !_players.Contains(attacker)
                || player.Pawn.Value == null)
            {
                return HookResult.Continue;
            }
            if (_knifes.Any(item => @event.Weapon.Contains(item, StringComparison.OrdinalIgnoreCase)))
            {
                player.Pawn.Value.Health -= 9999;
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            }
            return HookResult.Continue;
        }
    }
}
