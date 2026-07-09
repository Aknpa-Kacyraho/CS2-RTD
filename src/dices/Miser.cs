using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Miser : DiceBlueprint
    {
        public override string ClassName => "Miser";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, int> _startingMoney = [];
        private readonly Dictionary<CCSPlayerController, float> _reductionCache = [];

        public Miser(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Bank");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "资本要塞", "资本要塞联动生效！");

            if (player.InGameMoneyServices != null)
                _startingMoney[player] = player.InGameMoneyServices.Account;

            _reductionCache[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _startingMoney.Remove(player);
            _ = _reductionCache.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _startingMoney.Clear();
            _reductionCache.Clear();
        }

        private float GetReduction(CCSPlayerController player)
        {
            if (player.InGameMoneyServices == null) return 0f;
            int start = _startingMoney.TryGetValue(player, out int s) ? s : 0;
            int current = player.InGameMoneyServices.Account;
            int spent = start - current;
            if (spent <= 0) return 0f;

            int threshold = _config.Dices.Miser.Threshold;
            float stepPct = _config.Dices.Miser.ReductionPerStep;
            float maxPct = _config.Dices.Miser.MaxReduction;

            int steps = spent / threshold;
            float reduction = Math.Min(steps * stepPct, maxPct);
            return reduction;
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;
            if (victim.PlayerPawn?.Value == null || !victim.PlayerPawn.Value.IsValid) return HookResult.Continue;

            float reduction = GetReduction(victim);
            if (reduction > 0.001f)
            {
                info.Damage *= (1f - (_comboActive ? Math.Min(reduction * 1.5f, 0.75f) : reduction));
                float cache = _reductionCache.TryGetValue(victim, out float c) ? c : 0f;
                if (Math.Abs(reduction - cache) > 0.01f)
                {
                    _reductionCache[victim] = reduction;
                    int pct = (int)(reduction * 100);
                    victim.PrintToCenterAlert($"💰 吝啬减伤 {pct}%");
                }
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
    }
}
