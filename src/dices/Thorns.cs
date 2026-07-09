using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Thorns : DiceBlueprint
    {
        public override string ClassName => "Thorns";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        private readonly Dictionary<CCSPlayerController, int> _reflectDamage = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Thorns(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _comboActive = DiceSynergy.HasPartner(player, "GuardianAngel") || DiceSynergy.HasPartner(player, "MagneticPulse");
            int reflect = _random.Next(_config.Dices.Thorns.ReflectDamageMin, _config.Dices.Thorns.ReflectDamageMax + 1) + (_comboActive ? 10 : 0);
            _players.Add(player);
            if (DiceSynergy.HasPartner(player, "GuardianAngel"))
                DiceSynergy.AnnounceCombo(player, "圣光荆棘", "圣光荆棘联动生效！");
            if (DiceSynergy.HasPartner(player, "MagneticPulse"))
                DiceSynergy.AnnounceCombo(player, "磁力荆棘", "魔镜反弹+磁力脉冲！双重重压！");
            _reflectDamage[player] = reflect;
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "damage", reflect.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _reflectDamage.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _reflectDamage.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null
                || !entity.IsValid
                || info.Attacker.Value == null
                || !info.Attacker.Value.IsValid)
            {
                return HookResult.Continue;
            }

            // Find victim
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null
                || !victim.IsValid
                || !_reflectDamage.TryGetValue(victim, out int dmg))
            {
                return HookResult.Continue;
            }

            // Don't reflect self-damage
            if (info.Attacker.Value.Index == entity.Index)
            {
                return HookResult.Continue;
            }

            // Reflect the exact same damage back
            int reflectedDmg = (int)info.Damage;
            Server.NextFrame(() =>
            {
                CCSPlayerPawn? attackerPawn = info.Attacker.Value?.As<CCSPlayerPawn>();
                if (attackerPawn == null || !attackerPawn.IsValid || attackerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    return;

                attackerPawn.Health -= reflectedDmg;
                Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");

                CCSPlayerController? attacker = attackerPawn.Controller?.Value?.As<CCSPlayerController>();
                attacker?.PrintToCenterAlert($"🪞 魔镜反弹 -{reflectedDmg}!");
                victim?.PrintToCenterAlert($"🪞 反弹 {reflectedDmg} 伤害!");
            });

            return HookResult.Continue;
        }
    }
}
