using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Adrenaline : DiceBlueprint
    {
        public override string ClassName => "Adrenaline";
        public override List<string> Listeners => ["OnTick"];
        private readonly Dictionary<CCSPlayerController, (bool Active, float Speed, float Reduction)> _adrenalineState = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private bool _comboActive;

        public Adrenaline(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _players.Add(player);
            _adrenalineState[player] = (false, 1f, 0f);

            _comboActive = DiceSynergy.HasPartner(player, "Berserker") || DiceSynergy.HasPartner(player, "Overheat");
            if (_comboActive)
            {
                if (DiceSynergy.HasPartner(player, "Berserker"))
                    DiceSynergy.AnnounceCombo(player, "狂暴血脉", "肾上腺素速度额外+50%！狂战士的怒火在燃烧");
                if (DiceSynergy.HasPartner(player, "Overheat"))
                    DiceSynergy.AnnounceCombo(player, "狂热", "红温速度获取翻倍，上限翻倍！");
            }

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _adrenalineState.Remove(player);
            SpeedBonusManager.Unregister(player, "Adrenaline");
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                SpeedBonusManager.Unregister(player, "Adrenaline");
                _players.Remove(player);
            }
            _players.Clear();
            _adrenalineState.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_adrenalineState.Count == 0) return;

            foreach (CCSPlayerController player in _players.ToList())
            {
                try
                {
                    if (player == null
                        || !player.IsValid
                        || player.PlayerPawn?.Value == null
                        || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        continue;
                    }

                    if (!_adrenalineState.TryGetValue(player, out var state))
                    {
                        continue;
                    }

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    float hpPercent = (float)pawn.Health / Math.Max(pawn.MaxHealth, 1);
                    bool shouldBoost = hpPercent <= _config.Dices.Adrenaline.HpThresholdPercent;

                    if (shouldBoost && !state.Active)
                    {
                        float speed = _config.Dices.Adrenaline.SpeedMultiplierMin +
                            (float)_random.NextDouble() * (_config.Dices.Adrenaline.SpeedMultiplierMax - _config.Dices.Adrenaline.SpeedMultiplierMin);
                        float reduction = _config.Dices.Adrenaline.ReductionMin +
                            (float)_random.NextDouble() * (_config.Dices.Adrenaline.ReductionMax - _config.Dices.Adrenaline.ReductionMin);
                        if (_comboActive) speed *= 1.5f;
                        SpeedBonusManager.Register(player, "Adrenaline", speed - 1.0f);
                        DamageReductionManager.Register(player, "Adrenaline", reduction);
                        _adrenalineState[player] = (true, speed, reduction);
                        player.PrintToCenterAlert($"⚡ 肾上腺素! 速度 {(speed - 1f) * 100:F0}% 减伤 {(reduction * 100):F0}%!");
                    }
                    else if (!shouldBoost && state.Active)
                    {
                        SpeedBonusManager.Unregister(player, "Adrenaline");
                        DamageReductionManager.Unregister(player, "Adrenaline");
                        _adrenalineState[player] = (false, 1f, 0f);
                    }

                    float effective = SpeedBonusManager.GetEffective(player);
                    pawn.VelocityModifier = 1 + effective;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
                catch
                {
                    _adrenalineState.Remove(player);
                }
            }
        }
    }
}
