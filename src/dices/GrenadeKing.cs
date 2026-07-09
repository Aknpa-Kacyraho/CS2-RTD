using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class GrenadeKing : DiceBlueprint
    {
        public override string ClassName => "GrenadeKing";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnEntitySpawned",
            "OnPlayerTakeDamagePre"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<uint, (float Multiplier, float RadiusMult, CCSPlayerController Owner)> _trackedNades = [];

        public GrenadeKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Martyrdom");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "爆炸艺术家", "手雷伤害+50% 殉道爆炸范围翻倍");

            float min = _config.Dices.GrenadeKing.MultiplierMin;
            float max = _config.Dices.GrenadeKing.MultiplierMax;
            float multiplier = (float)Math.Round((_random.NextDouble() * (max - min)) + min, 2);

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "multiplier", multiplier.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _trackedNades.Clear();
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (entity.DesignerName != "hegrenade_projectile") return;

            Server.NextFrame(() =>
            {
                if (entity == null || !entity.IsValid) return;
                var nade = new CHEGrenadeProjectile(entity.Handle);
                if (!nade.IsValid) return;

                var throwerPawn = nade.Thrower?.Value;
                if (throwerPawn == null || !throwerPawn.IsValid) return;
                var thrower = throwerPawn.Controller?.Value?.As<CCSPlayerController>();
                if (thrower == null || !thrower.IsValid || !_players.Contains(thrower)) return;

                float min = _config.Dices.GrenadeKing.MultiplierMin;
                float max = _config.Dices.GrenadeKing.MultiplierMax;
                float multiplier = (float)Math.Round((_random.NextDouble() * (max - min)) + min, 2);
                float radiusMult = 1f + (multiplier - 1f) * 0.5f; // radius grows with damage multiplier
                if (_comboActive) { multiplier += 0.5f; radiusMult *= 2f; } // Martyrdom combo: +50% dmg, 2x radius

                nade.Damage *= multiplier;
                nade.DmgRadius *= radiusMult;
                _trackedNades[nade.Index] = (multiplier, radiusMult, thrower);
            });
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            if (info.Attacker?.Value == null) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            // Check if damage is from player's own HE grenade
            if (info.Inflictor?.Value != null && info.Inflictor.Value.DesignerName == "hegrenade_projectile")
            {
                if (_trackedNades.TryGetValue(info.Inflictor.Value.Index, out var nadeInfo))
                {
                    info.Damage *= nadeInfo.Multiplier;
                    return HookResult.Changed;
                }
            }

            return HookResult.Continue;
        }
    }
}
