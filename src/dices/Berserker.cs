using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Berserker : DiceBlueprint
    {
        public override string ClassName => "Berserker";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private bool _comboActive;

        public Berserker(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Adrenaline")
                        || DiceSynergy.HasPartner(player, "DamageMultiplier");

            DamageBonusManager.Register(player, "Berserker", 0f);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            DamageBonusManager.Unregister(player, "Berserker");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                DamageBonusManager.Unregister(p, "Berserker");
            _players.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid || info.Attacker.Value == null) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = attacker.PlayerPawn?.Value;
            if (attackerPawn == null || !attackerPawn.IsValid) return HookResult.Continue;

            float healthPercent = (float)attackerPawn.Health / Math.Max(attackerPawn.MaxHealth, 1);
            float maxMult = _config.Dices.Berserker.MaxMultiplier;
            float myBonus = (1.0f + (1.0f - healthPercent) * (maxMult - 1.0f)) - 1.0f;
            if (_comboActive && myBonus < 0.3f) myBonus = 0.3f;

            DamageBonusManager.Register(attacker, "Berserker", myBonus);

            if (DamageBonusManager.IsHighest(attacker, "Berserker"))
            {
                float effective = DamageBonusManager.GetEffective(attacker, maxMult - 1.0f);
                info.Damage = (int)(info.Damage * (1 + effective));
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
    }
}
