using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DeathKnight : DiceBlueprint
    {
        public override string ClassName => "DeathKnight";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private bool _comboActive;

        private readonly Dictionary<CCSPlayerController, float> _nextRegenTime = [];

        public DeathKnight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _nextRegenTime[player] = 0f;
            _comboActive = DiceSynergy.HasPartner(player, "Frostmourne");
            if (_comboActive)
            {
                var instance = RollTheDice.Instance;
                if (instance != null && instance.HasDiceActive(player, "Frostmourne"))
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
            _ = _players.Remove(player);
            _ = _nextRegenTime.Remove(player);
            DamageReductionManager.Unregister(player, "DeathKnight");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                DamageReductionManager.Unregister(p, "DeathKnight");
            _players.Clear();
            _nextRegenTime.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            CCSPlayerPawn pawn = victim.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;

            float hpLost = 1f - (float)pawn.Health / Math.Max(pawn.MaxHealth, 1);
            float reduction = Math.Min(hpLost, _config.Dices.DeathKnight.ReductionCap);
            DamageReductionManager.Register(victim, "DeathKnight", reduction);

            victim.PrintToCenterAlert($"💀 减伤 {(int)(reduction * 100)}%");

            float effective = DamageReductionManager.GetEffective(victim, _config.Dices.DeathKnight.ReductionCap);
            info.Damage = (int)(info.Damage * (1 - effective));
            return HookResult.Changed;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;

                bool holdingKnife = pawn.WeaponServices?.ActiveWeapon?.Value?.DesignerName?.Contains("knife", StringComparison.OrdinalIgnoreCase) == true;

                if (holdingKnife)
                {
                    if (!_nextRegenTime.TryGetValue(player, out float next) || now >= next)
                    {
                        _nextRegenTime[player] = now + 1f;
                        pawn.Health = Math.Min(pawn.Health + _config.Dices.DeathKnight.HpRegen, pawn.MaxHealth);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                }

                float hpLost = 1f - (float)pawn.Health / Math.Max(pawn.MaxHealth, 1);
                float reduction = Math.Min(hpLost, _config.Dices.DeathKnight.ReductionCap * (_comboActive ? 1.5f : 1f));
                DamageReductionManager.Register(player, "DeathKnight", reduction);
            }
        }
    }
}
