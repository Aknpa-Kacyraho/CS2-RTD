using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Frostmourne : DiceBlueprint
    {
        public override string ClassName => "Frostmourne";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private readonly Dictionary<CCSPlayerController, float> _lastHealTime = [];

        public Frostmourne(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _lastHealTime[player] = 0f;
            _comboActive = DiceSynergy.HasPartner(player, "DeathKnight");
            if (_comboActive)
            {
                var instance = RollTheDice.Instance;
                if (instance != null && instance.HasDiceActive(player, "DeathKnight"))
                {
                    DiceSynergy.AnnounceCombo(player, "死亡骑士完全体", "死亡骑士+霜之哀伤合成为死亡骑士完全体！");
                    var captured = player;
                    Server.NextFrame(() =>
                    {
                        if (instance != null && captured.IsValid)
                        {
                            instance.RemoveDiceFromPlayer(captured, "DeathKnight");
                            instance.RemoveDiceFromPlayer(captured, "Frostmourne");
                            instance.ForceDiceForPlayer(captured, "DeathKnightComplete");
                        }
                    });
                    return;
                }
                else
                    DiceSynergy.AnnounceCombo(player, "死亡骑士完全体", "团队联动！死亡骑士与霜之哀伤共鸣！");
            }
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DamageReductionManager.Unregister(player, "Frostmourne");
            _ = _players.Remove(player);
            _ = _lastHealTime.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) DamageReductionManager.Unregister(p, "Frostmourne");
            _players.Clear();
            _lastHealTime.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            bool hasKnife = false;
            if (victim.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value?.DesignerName is string wn && wn.Contains("knife"))
                hasKnife = true;

            if (hasKnife)
            {
                float reduction = _config.Dices.Frostmourne.KnifeDamageReduction;
                info.Damage = (int)(info.Damage * (1 - reduction));
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                bool hasKnife = false;
                if (player.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value?.DesignerName is string wn && wn.Contains("knife"))
                    hasKnife = true;

                if (!hasKnife) continue;

                if (_lastHealTime.TryGetValue(player, out float lastHeal) && now - lastHeal >= 1.0f)
                {
                    _lastHealTime[player] = now;
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    pawn.Health = Math.Min(pawn.Health + _config.Dices.Frostmourne.HealPerSec, pawn.MaxHealth);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }
        }
    }
}
