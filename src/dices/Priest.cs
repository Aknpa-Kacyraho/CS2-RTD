using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Priest : DiceBlueprint
    {
        public override string ClassName => "Priest";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private bool _comboActive;

        public Priest(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            player.PlayerPawn.Value.VelocityModifier = 1.2f;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");

            _comboActive = DiceSynergy.HasPartner(player, "Pope");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "神圣共鸣", "牧师20%移速+治疗翻倍！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                if (p?.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            _players.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (info.Attacker?.Value == null) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid || attacker == victim)
                return HookResult.Continue;
            if (attacker.TeamNum != victim.TeamNum) return HookResult.Continue;
            if (victim.PlayerPawn?.Value == null || !victim.PlayerPawn.Value.IsValid) return HookResult.Continue;

            int healAmount = _comboActive ? _config.Dices.Priest.HealAmount * 2 : _config.Dices.Priest.HealAmount;
            info.Damage = 0;

            CCSPlayerPawn pawn = victim.PlayerPawn.Value;
            int newHealth = Math.Min(pawn.Health + healAmount, pawn.MaxHealth);
            pawn.Health = newHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            victim.PrintToCenterAlert($"✚ +{healAmount} HP (牧师治疗)");
            return HookResult.Changed;
        }
    }
}
