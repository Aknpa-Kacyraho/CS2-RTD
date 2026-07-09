using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class WolfKing : DiceBlueprint
    {
        public override string ClassName => "WolfKing";

        // === v3.0 special dice properties ===
        public override float Weight => 1.0f;
        public override bool IsSpecial => true;
        public override float SecondRoundProbability => 0.9f;
        public override string? SecondRoundRewardId => "Wolf";

        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        public WolfKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.MaxHealth = _config.Dices.WolfKing.HP;
            pawn.Health = _config.Dices.WolfKing.HP;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            DamageBonusManager.Register(player, "WolfKing", _config.Dices.WolfKing.DamageBonus);
            SpeedBonusManager.Register(player, "WolfKing", _config.Dices.WolfKing.SpeedBonus);

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🐺 {player.PlayerName} 成为狼王！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            DamageBonusManager.Unregister(player, "WolfKing");
            SpeedBonusManager.Unregister(player, "WolfKing");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                DamageBonusManager.Unregister(p, "WolfKing");
                SpeedBonusManager.Unregister(p, "WolfKing");
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
                float effective = SpeedBonusManager.GetEffective(player, _config.Dices.WolfKing.SpeedBonus);
                player.PlayerPawn.Value.VelocityModifier = 1 + effective;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            if (DamageBonusManager.IsHighest(attacker, "WolfKing"))
            {
                float effective = DamageBonusManager.GetEffective(attacker, _config.Dices.WolfKing.DamageBonus);
                info.Damage = (int)(info.Damage * (1 + effective));
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }
    }
}
