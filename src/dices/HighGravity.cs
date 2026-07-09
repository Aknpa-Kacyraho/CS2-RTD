using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class HighGravity : DiceBlueprint
    {
        public override string ClassName => "HighGravity";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        public readonly Random _random = new();

        public HighGravity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid)
                return;

            ChangePlayerGravity(player, _config.Dices.HighGravity.GravityScale);
            DamageReductionManager.Register(player, "HighGravity", _config.Dices.HighGravity.DamageReduction);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            ChangePlayerGravity(player, 1f);
            DamageReductionManager.Unregister(player, "HighGravity");
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                DamageReductionManager.Unregister(player, "HighGravity");
                Remove(player);
            }
            _players.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            float reduction = DamageReductionManager.GetEffective(victim, _config.Dices.HighGravity.DamageReduction);
            info.Damage = (int)(info.Damage * (1 - reduction));
            return HookResult.Changed;
        }

        private static void ChangePlayerGravity(CCSPlayerController? player, float gravityScale)
        {
            if (player?.Pawn?.Value != null && player.Pawn.Value.IsValid)
                player.Pawn.Value.ActualGravityScale = gravityScale;
        }
    }
}
