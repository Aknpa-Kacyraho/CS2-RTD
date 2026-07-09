using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Taotie : DiceBlueprint
    {
        public override string ClassName => "Taotie";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        private readonly HashSet<ulong> _alreadyStolenThisTick = [];

        public Taotie(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
        public override void Reset() { _players.Clear(); _alreadyStolenThisTick.Clear(); }
        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || attacker == victim || victim.IsHLTV)
                return HookResult.Continue;

            if (_alreadyStolenThisTick.Contains(victim.SteamID))
                return HookResult.Continue;

            _alreadyStolenThisTick.Add(victim.SteamID);

            CCSPlayerPawn pawn = attacker.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;

            var instance = RollTheDice.Instance;
            if (instance != null)
            {
                var victimDices = instance.GetAllDiceForPlayer(victim);
                if (victimDices != null && victimDices.Count > 0)
                {
                    foreach (var dice in victimDices)
                        instance.RemoveDiceFromPlayer(victim, dice);

                    pawn.Health += _config.Dices.Taotie.HpPerEat;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    attacker.PrintToCenterAlert($"🍖 饕餮吞噬！+{_config.Dices.Taotie.HpPerEat}HP！");
                    victim.PrintToCenterAlert("🍖 被饕餮吞噬了骰子！");
                }
            }

            return HookResult.Continue;
        }
    }
}
