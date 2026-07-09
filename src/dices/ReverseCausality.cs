using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ReverseCausality : DiceBlueprint
    {
        public override string ClassName => "ReverseCausality";
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private struct PendingDamage
        {
            public float ApplyTime;
            public int Damage;
        }

        private static readonly Dictionary<ulong, List<PendingDamage>> _delayedDamage = [];
        // Track when each player's double damage window expires (latest pending damage time)
        private static readonly Dictionary<ulong, float> _doubleDamageUntil = [];

        public ReverseCausality(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
            : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            // Initialize empty queue for this player
            if (!_delayedDamage.ContainsKey(player.SteamID))
                _delayedDamage[player.SteamID] = [];
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            // Apply all pending damage immediately on dice removal
            ApplyAllPending(player.SteamID);
            DamageBonusManager.UnregisterBySteamId(player.SteamID, "ReverseCausality");
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            // Apply all pending damage for all dice holders
            foreach (var p in _players.ToList())
            {
                ApplyAllPending(p.SteamID);
                DamageBonusManager.UnregisterBySteamId(p.SteamID, "ReverseCausality");
            }
            _players.Clear();
            _delayedDamage.Clear();
            _doubleDamageUntil.Clear();
        }

        public override void Destroy() => Reset();

        private void ApplyAllPending(ulong steamId)
        {
            if (!_delayedDamage.TryGetValue(steamId, out var queue) || queue.Count == 0)
                return;

            int total = 0;
            foreach (var pd in queue) total += pd.Damage;
            queue.Clear();

            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.Health -= total;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            player.PrintToCenterAlert($"⏳ 因果结算！-{total}HP！");
            if (pawn.Health <= 0)
            {
                if (!player.IsBot)
                    pawn.CommitSuicide(false, true);
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            if (info.Damage <= 0) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            float delay = _config.Dices.ReverseCausality.DelaySeconds;
            float applyTime = now + delay;

            // Queue the damage
            if (!_delayedDamage.ContainsKey(victim.SteamID))
                _delayedDamage[victim.SteamID] = [];
            _delayedDamage[victim.SteamID].Add(new PendingDamage { ApplyTime = applyTime, Damage = (int)info.Damage });

            // Extend double damage window
            _doubleDamageUntil[victim.SteamID] = Math.Max(
                _doubleDamageUntil.TryGetValue(victim.SteamID, out float existing) ? existing : 0,
                applyTime);

            // Register double damage bonus
            float bonus = _config.Dices.ReverseCausality.DamageMultiplier;
            DamageBonusManager.RegisterBySteamId(victim.SteamID, "ReverseCausality", bonus);

            // Cancel the immediate damage
            info.Damage = 0;
            victim.PrintToCenterAlert($"⏳ 因果倒置！伤害延迟{delay:F0}s，期间伤害翻倍！");

            return HookResult.Changed;
        }

        // Player dies while having pending damage - apply immediately
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? deadPlayer = @event.Userid;
            if (deadPlayer == null || !deadPlayer.IsValid) return HookResult.Continue;
            if (!_players.Contains(deadPlayer)) return HookResult.Continue;

            // Don't apply pending damage if already dead (avoid double-kill loops)
            // Just clean up
            ApplyAllPending(deadPlayer.SteamID);
            _doubleDamageUntil.Remove(deadPlayer.SteamID);

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            float now = (float)Server.CurrentTime;

            // Check if any dice holders are dead (disconnected mid-round etc.)
            foreach (var steamId in _delayedDamage.Keys.ToList())
            {
                var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    // Player is dead or disconnected — apply pending damage immediately to prevent orphaned state
                    continue; // Don't auto-apply; damage was already applied in EventPlayerDeath
                }
            }

            // Apply expired delayed damage for each holder
            foreach (var steamId in _players.Select(p => p.SteamID).ToList())
            {
                if (!_delayedDamage.TryGetValue(steamId, out var queue) || queue.Count == 0)
                    continue;

                var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
                if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                int totalApplied = 0;

                for (int i = queue.Count - 1; i >= 0; i--)
                {
                    if (now >= queue[i].ApplyTime)
                    {
                        totalApplied += queue[i].Damage;
                        queue.RemoveAt(i);
                    }
                }

                if (totalApplied > 0)
                {
                    pawn.Health -= totalApplied;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    player.PrintToCenterAlert($"⏳ 因果倒置！-{totalApplied}HP！");

                    if (pawn.Health <= 0)
                    {
                        if (!player.IsBot)
                            pawn.CommitSuicide(false, true);
                    }
                }

                // Check if double damage window has expired
                if (_doubleDamageUntil.TryGetValue(steamId, out float windowEnd) && now >= windowEnd && queue.Count == 0)
                {
                    DamageBonusManager.UnregisterBySteamId(steamId, "ReverseCausality");
                    _doubleDamageUntil.Remove(steamId);
                }
            }
        }
    }
}
