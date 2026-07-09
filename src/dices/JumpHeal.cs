using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class JumpHeal : DiceBlueprint
    {
        public override string ClassName => "JumpHeal";
        public override List<string> Listeners => ["OnTick"];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private bool _comboActive;
        private readonly Dictionary<CCSPlayerController, bool> _wasOnGround = [];

        public JumpHeal(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _wasOnGround[player] = true;
            _comboActive = DiceSynergy.HasPartner(player, "Regeneration");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _wasOnGround.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _wasOnGround.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            foreach (var player in _players.ToList())
            {
                if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                bool onGround = pawn.GroundEntity != null && pawn.GroundEntity.IsValid;
                bool wasGround = _wasOnGround.GetValueOrDefault(player, true);
                _wasOnGround[player] = onGround;

                if (wasGround && !onGround)
                {
                    int heal = _random.Next(_config.Dices.JumpHeal.HealMin, _config.Dices.JumpHeal.HealMax + 1);
                    if (_comboActive) heal *= 2;
                    int newHp = Math.Min(pawn.Health + heal, pawn.MaxHealth);
                    int actual = newHp - pawn.Health;
                    if (actual <= 0) continue;

                    pawn.Health = newHp;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    player.PrintToCenterAlert($"🍬 +{actual} HP");
                }
            }
        }
    }
}
