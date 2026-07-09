using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Fibonacci : DiceBlueprint
    {
        public override string ClassName => "Fibonacci";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        private static readonly HashSet<int> FibNumbers = [1, 2, 3, 5, 8, 13, 21, 34, 55];

        public Fibonacci(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic) { _ = _players.Remove(player); }
        public override void Reset() { _players.Clear(); }
        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            int dmg = (int)info.Damage;
            if (FibNumbers.Contains(dmg))
            {
                info.Damage = 0;
                CCSPlayerPawn pawn = victim.PlayerPawn?.Value;
                if (pawn != null && pawn.IsValid)
                {
                    pawn.Health += 89;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
                victim.PrintToCenterAlert($"🔄 斐波那契！免疫{dmg}伤害，回复89HP！");
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
    }
}
