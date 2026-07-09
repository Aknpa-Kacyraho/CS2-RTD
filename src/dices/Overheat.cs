using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Overheat : DiceBlueprint
    {
        public override string ClassName => "Overheat";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        public override List<string> Events => ["EventPlayerDeath"];
        private readonly Dictionary<CCSPlayerController, bool> _comboActive = [];

        private readonly Dictionary<CCSPlayerController, int> _stacks = [];
        private readonly Dictionary<CCSPlayerController, float> _nextTickTime = [];

        public Overheat(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _stacks[player] = 0;
            _nextTickTime[player] = (float)Server.CurrentTime + _config.Dices.Overheat.Interval;
            bool hasCombo = DiceSynergy.HasPartner(player, "Adrenaline");
            _comboActive[player] = hasCombo;
            if (hasCombo)
                DiceSynergy.AnnounceCombo(player, "狂热", "速度获取翻倍，上限翻倍！");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _stacks.Remove(player);
            _ = _nextTickTime.Remove(player);
            _ = _comboActive.Remove(player);
            DamageBonusManager.Unregister(player, "Overheat");

            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
            {
                DamageBonusManager.Unregister(player, "Overheat");
                if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    player.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            _players.Clear();
            _stacks.Clear();
            _nextTickTime.Clear();
            _comboActive.Clear();
        }

        public override void Destroy() => Reset();

        private (float speedPerStack, float damagePerStack, int maxStacks) GetComboParams(CCSPlayerController player)
        {
            bool combo = _comboActive.TryGetValue(player, out bool c) && c;
            float speed = combo ? _config.Dices.Overheat.SpeedPerStack * 2f : _config.Dices.Overheat.SpeedPerStack;
            float damage = combo ? _config.Dices.Overheat.DamagePerStack * 2f : _config.Dices.Overheat.DamagePerStack;
            int max = combo ? _config.Dices.Overheat.MaxStacks * 2 : _config.Dices.Overheat.MaxStacks;
            return (speed, damage, max);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker) || attacker == @event.Userid)
                return HookResult.Continue;

            if (_stacks.TryGetValue(attacker, out int current) && current > 0)
            {
                int reduction = Math.Max(1, (int)(_config.Dices.Overheat.MaxStacks * 0.1f));
                _stacks[attacker] = Math.Max(0, current - reduction);
                if (attacker.PlayerPawn?.Value != null && attacker.PlayerPawn.Value.IsValid)
                {
                    var (spd, _, _) = GetComboParams(attacker);
                    float speed = 1.0f + _stacks[attacker] * spd;
                    attacker.PlayerPawn.Value.VelocityModifier = speed;
                    Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
                attacker.PrintToCenterAlert($"红温降低！-{reduction}层 (剩余{_stacks[attacker]}/14)");
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_stacks.Count == 0) return;

            float now = (float)Server.CurrentTime;
            float interval = _config.Dices.Overheat.Interval;

            foreach (var kv in _nextTickTime.ToList())
            {
                var player = kv.Key;
                if (now >= kv.Value)
                {
                    _nextTickTime[player] = now + interval;

                    if (!_stacks.TryGetValue(player, out int stack))
                        stack = 0;

                    var (speedPerStack, damagePerStack, maxStacks) = GetComboParams(player);

                    if (stack < maxStacks)
                    {
                        stack++;
                        _stacks[player] = stack;

                        if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                        {
                            float speed = 1.0f + stack * speedPerStack;
                            player.PlayerPawn.Value.VelocityModifier = speed;
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");

                            float dmgPct = stack * damagePerStack;
                            DamageBonusManager.Register(player, "Overheat", dmgPct);

                            if (stack % 2 == 0 || stack >= maxStacks - 2)
                                player.PrintToCenterAlert($"红温: {stack}/{maxStacks}层 速度+{stack * speedPerStack * 100:F0}% 伤害+{dmgPct * 100:F0}%");
                        }
                    }
                    else
                    {
                        if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                        {
                            float speed = 1.0f + maxStacks * speedPerStack;
                            if (player.PlayerPawn.Value.VelocityModifier != speed)
                            {
                                player.PlayerPawn.Value.VelocityModifier = speed;
                                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                            }
                        }
                    }
                }
            }

            // Maintain speed for all players (engine may reset)
            foreach (var player in _players)
            {
                if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (!_stacks.TryGetValue(player, out int stack)) continue;

                var (speedPerStack, _, _) = GetComboParams(player);
                float expected = 1.0f + stack * speedPerStack;
                if (player.PlayerPawn.Value.VelocityModifier != expected)
                {
                    player.PlayerPawn.Value.VelocityModifier = expected;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (DamageBonusManager.IsHighest(attacker, "Overheat"))
            {
                float effective = DamageBonusManager.GetEffective(attacker);
                info.Damage = (int)(info.Damage * (1 + effective));
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }
    }
}
