using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Evasion : DiceBlueprint
    {
        public override string ClassName => "Evasion";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, float> _dodgeChance = [];

        public Evasion(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // Random dodge chance between min and max
            Random rng = new(Guid.NewGuid().GetHashCode());
            float chance = _config.Dices.Evasion.DodgeChanceMin +
                (float)rng.NextDouble() * (_config.Dices.Evasion.DodgeChanceMax - _config.Dices.Evasion.DodgeChanceMin);
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Shield");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "钢铁壁垒", "钢铁壁垒联动生效！");
            _dodgeChance[player] = chance;
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "chance", (chance * 100).ToString("F0") }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _dodgeChance.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _dodgeChance.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid)
            {
                return HookResult.Continue;
            }

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null
                || !victim.IsValid
                || !_dodgeChance.TryGetValue(victim, out float chance))
            {
                return HookResult.Continue;
            }

            if (_random.NextDouble() < (_comboActive ? Math.Min(chance + 0.1f, 1f) : chance))
            {
                info.Damage = 0;
                victim.PrintToCenterAlert($"↗ 闪避! ({chance * 100:F0}%)");
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
    }
}
