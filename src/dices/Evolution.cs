using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Evolution : DiceBlueprint
    {
        public override string ClassName => "Evolution";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, float> _nextEvolveTime = [];
        private readonly Dictionary<CCSPlayerController, int> _damageStacks = [];
        private readonly Dictionary<CCSPlayerController, int> _speedStacks = [];
        private readonly Dictionary<CCSPlayerController, int> _hpStacks = [];

        public Evolution(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Awakener");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "超进化", "超进化联动生效！");
            float interval = _comboActive ? _config.Dices.Evolution.EvolveInterval / 2f : _config.Dices.Evolution.EvolveInterval;
            _nextEvolveTime[player] = (float)Server.CurrentTime + interval;
            _damageStacks[player] = 0;
            _speedStacks[player] = 0;
            _hpStacks[player] = 0;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🧬 进化开始！每25秒随机提升属性！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            // Death resets all stacks
            RevertPlayer(player);
            _ = _players.Remove(player);
            _ = _nextEvolveTime.Remove(player);
            _ = _damageStacks.Remove(player);
            _ = _speedStacks.Remove(player);
            _ = _hpStacks.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) RevertPlayer(p);
            _players.Clear(); _nextEvolveTime.Clear();
            _damageStacks.Clear(); _speedStacks.Clear(); _hpStacks.Clear();
        }

        public override void Destroy() => Reset();

        private void RevertPlayer(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                    if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        // Death resets
                        RevertPlayer(player);
                        _ = _players.Remove(player);
                        _ = _nextEvolveTime.Remove(player);
                        _ = _damageStacks.Remove(player);
                        _ = _speedStacks.Remove(player);
                        _ = _hpStacks.Remove(player);
                        continue;
                    }

                    if (!_nextEvolveTime.TryGetValue(player, out float next) || now < next) continue;

                    int maxStacks = _config.Dices.Evolution.MaxStacks;
                    int totalStacks = _damageStacks.GetValueOrDefault(player) + _speedStacks.GetValueOrDefault(player) + _hpStacks.GetValueOrDefault(player);
                    if (totalStacks >= maxStacks * 3)
                    {
                        float maxInterval = _comboActive ? _config.Dices.Evolution.EvolveInterval / 2f : _config.Dices.Evolution.EvolveInterval;
                        _nextEvolveTime[player] = now + maxInterval;
                        continue;
                    }

                    // Random pick: 0=damage, 1=speed, 2=HP
                    int roll = _random.Next(3);
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;

                    string evolveType;
                    if (roll == 0)
                    {
                        int ds = _damageStacks.GetValueOrDefault(player) + 1;
                        _damageStacks[player] = ds;
                        evolveType = $"伤害+{_config.Dices.Evolution.DamagePerStack * 100:F0}%";
                    }
                    else if (roll == 1)
                    {
                        int ss = _speedStacks.GetValueOrDefault(player) + 1;
                        _speedStacks[player] = ss;
                        float speedMult = 1.0f + _config.Dices.Evolution.SpeedPerStack * ss;
                        pawn.VelocityModifier = speedMult;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                        evolveType = $"移速+{_config.Dices.Evolution.SpeedPerStack * 100:F0}%";
                    }
                    else
                    {
                        int hs = _hpStacks.GetValueOrDefault(player) + 1;
                        _hpStacks[player] = hs;
                        int hpBonus = _config.Dices.Evolution.HpPerStack;
                        pawn.MaxHealth += hpBonus;
                        pawn.Health += hpBonus;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                        evolveType = $"HP上限+{hpBonus}";
                    }

                    float nextInterval = _comboActive ? _config.Dices.Evolution.EvolveInterval / 2f : _config.Dices.Evolution.EvolveInterval;
                        _nextEvolveTime[player] = now + nextInterval;
                    player.PrintToCenterAlert($"🧬 进化！{evolveType} | 总层数：{_damageStacks.GetValueOrDefault(player)}+{_speedStacks.GetValueOrDefault(player)}+{_hpStacks.GetValueOrDefault(player)}");
                }
                catch { }
            }

            // Maintain speed for evolved players
            foreach (var kv in _speedStacks)
            {
                var player = kv.Key;
                if (kv.Value <= 0) continue;
                if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    float expected = 1.0f + _config.Dices.Evolution.SpeedPerStack * kv.Value;
                    if (Math.Abs(player.PlayerPawn.Value.VelocityModifier - expected) > 0.01f)
                    {
                        player.PlayerPawn.Value.VelocityModifier = expected;
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                    }
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_damageStacks.Count == 0) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_damageStacks.TryGetValue(attacker, out int ds) || ds <= 0)
                return HookResult.Continue;

            float mult = 1.0f + _config.Dices.Evolution.DamagePerStack * ds;
            info.Damage *= mult;
            return HookResult.Changed;
        }
    }
}
