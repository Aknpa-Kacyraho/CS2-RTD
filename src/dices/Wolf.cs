using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Wolf : DiceBlueprint
    {
        public override string ClassName => "Wolf";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private float _roundStartTime;
        private bool _bonusApplied;

        public Wolf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            if (_roundStartTime == 0) _roundStartTime = (float)Server.CurrentTime;
            _bonusApplied = false;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            DamageBonusManager.Unregister(player, "Wolf");
            SpeedBonusManager.Unregister(player, "Wolf");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                DamageBonusManager.Unregister(p, "Wolf");
                SpeedBonusManager.Unregister(p, "Wolf");
            }
            _players.Clear();
            _roundStartTime = 0;
            _bonusApplied = false;
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            if (!_bonusApplied && now - _roundStartTime >= _config.Dices.Wolf.BonusDelay)
            {
                _bonusApplied = true;
                ApplyWolfPackBonuses();
            }

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                float effective = SpeedBonusManager.GetEffective(player, 0.5f);
                player.PlayerPawn.Value.VelocityModifier = 1 + effective;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        private void ApplyWolfPackBonuses()
        {
            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;

                int wolfCount = _players.Count(p => p.IsValid && p.TeamNum == player.TeamNum);
                int hpBonus = _config.Dices.Wolf.HpPerWolf * wolfCount;
                int armorBonus = _config.Dices.Wolf.ArmorPerWolf * wolfCount;
                float dmgBonus = _config.Dices.Wolf.DamagePerWolf * wolfCount;
                float speedBonus = _config.Dices.Wolf.SpeedPerWolf * wolfCount;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                pawn.MaxHealth += hpBonus;
                pawn.Health += hpBonus;
                pawn.ArmorValue = Math.Min(pawn.ArmorValue + armorBonus, 100);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

                DamageBonusManager.Register(player, "Wolf", dmgBonus);
                SpeedBonusManager.Register(player, "Wolf", speedBonus);

                player.PrintToCenterAlert($"🐺 狼群之力！+{hpBonus}HP +{armorBonus}甲 +{(int)(dmgBonus*100)}%伤害 +{(int)(speedBonus*100)}%速度！({wolfCount}只狼)");
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            if (DamageBonusManager.IsHighest(attacker, "Wolf"))
            {
                float effective = DamageBonusManager.GetEffective(attacker);
                info.Damage = (int)(info.Damage * (1 + effective));
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }
    }
}
