using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Paladin : DiceBlueprint
    {
        public override string ClassName => "Paladin";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, float> _paladinSpeedBonus = [];
        private readonly Dictionary<CCSPlayerController, int> _paladinHpBonus = [];

        public Paladin(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _paladinSpeedBonus[player] = 0f;
            _paladinHpBonus[player] = 0;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            int armor = _config.Dices.Paladin.BonusArmor;
            pawn.ArmorValue = Math.Max(pawn.ArmorValue, armor);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🛡 圣骑士！每次护甲受损强化自己...");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            Revert(player);
            _ = _players.Remove(player);
            _ = _paladinSpeedBonus.Remove(player);
            _ = _paladinHpBonus.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Revert(p);
            _players.Clear();
            _paladinSpeedBonus.Clear();
            _paladinHpBonus.Clear();
        }

        private void Revert(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            CCSPlayerPawn pawn = victim.PlayerPawn!.Value!;
            int armorBefore = pawn.ArmorValue;
            if (armorBefore > 0 && info.Damage > 0)
            {
                float currentSpeed = _paladinSpeedBonus.TryGetValue(victim, out float spd) ? spd : 0f;
                int currentHp = _paladinHpBonus.TryGetValue(victim, out int hp) ? hp : 0;

                if (currentSpeed < 0.5f)
                {
                    currentSpeed = Math.Min(currentSpeed + 0.1f, 0.5f);
                    _paladinSpeedBonus[victim] = currentSpeed;
                }

                currentHp += 10;
                _paladinHpBonus[victim] = currentHp;

                pawn.VelocityModifier = 1.0f + currentSpeed;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

                pawn.MaxHealth += 10;
                pawn.Health += 10;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                victim.PrintToCenterAlert($"🛡 圣骑士强化！速度 ×{1.0f + currentSpeed:F1} | HP +{currentHp}");
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                    if (!_paladinSpeedBonus.TryGetValue(player, out float speedBonus) || speedBonus <= 0f) continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    float targetSpeed = 1.0f + speedBonus;
                    if (Math.Abs(pawn.VelocityModifier - targetSpeed) > 0.01f)
                    {
                        pawn.VelocityModifier = targetSpeed;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    }
                }
                catch { }
            }
        }
    }
}
