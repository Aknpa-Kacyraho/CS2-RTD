using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System;

namespace RollTheDice.Dices
{
    public class Lucky : DiceBlueprint
    {
        public override string ClassName => "Lucky";
        public override List<string> Listeners => [
            "OnTick",
            "OnPlayerTakeDamagePre"
        ];
        private readonly Dictionary<CCSPlayerController, float> _nextTickTime = [];
        private readonly Dictionary<CCSPlayerController, float> _interval = [];
        private readonly Dictionary<CCSPlayerController, (float EndTime, float Multiplier, string Type)> _activeBuffs = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Lucky(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            float iv = _config.Dices.Lucky.IntervalMin +
                (float)_random.NextDouble() * (_config.Dices.Lucky.IntervalMax - _config.Dices.Lucky.IntervalMin);
            _players.Add(player);
            _nextTickTime[player] = (float)Server.CurrentTime + iv;
            _interval[player] = iv;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player); _ = _nextTickTime.Remove(player); _ = _interval.Remove(player);
            _ = _activeBuffs.Remove(player);
            // Reset speed on removal
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                if (p?.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            _players.Clear(); _nextTickTime.Clear(); _interval.Clear(); _activeBuffs.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_activeBuffs.ContainsKey(attacker))
                return HookResult.Continue;

            if (_activeBuffs.TryGetValue(attacker, out var buff) && buff.Type == "damage" && Server.CurrentTime < buff.EndTime)
            {
                info.Damage *= buff.Multiplier;
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            // Expire old buffs
            var expired = _activeBuffs.Where(kv => now >= kv.Value.EndTime).Select(kv => kv.Key).ToList();
            foreach (var p in expired)
            {
                _activeBuffs.Remove(p);
                if (p?.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }

            // Maintain active speed buffs
            foreach (var (player, buff) in _activeBuffs)
            {
                if (buff.Type == "speed" && player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    player.PlayerPawn.Value.VelocityModifier = buff.Multiplier;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }

            if (_nextTickTime.Count == 0) return;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                        || player.InGameMoneyServices == null) continue;
                    if (!_nextTickTime.TryGetValue(player, out float next) || next > now) continue;

                    // Money reward
                    int money = _random.Next(_config.Dices.Lucky.MoneyMin, _config.Dices.Lucky.MoneyMax + 1);
                    player.InGameMoneyServices.Account += money;
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

                    // Random buff
                    string buffDesc = "";
                    int buffRoll = _random.Next(0, 4);
                    switch (buffRoll)
                    {
                        case 0: // Speed boost 1.2x for 5s
                            if (player.PlayerPawn.Value.IsValid)
                            {
                                player.PlayerPawn.Value.VelocityModifier = 1.2f;
                                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                                _activeBuffs[player] = (now + 5f, 1.2f, "speed");
                            }
                            buffDesc = "⚡加速";
                            break;
                        case 1: // Damage boost 1.2x for 5s
                            _activeBuffs[player] = (now + 5f, 1.2f, "damage");
                            buffDesc = "💪增伤";
                            break;
                        case 2: // HP restore +20
                            {
                                var pawn = player.PlayerPawn.Value;
                                pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + 20);
                                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                                buffDesc = "❤回血";
                            }
                            break;
                        case 3: // Armor restore +20
                            {
                                var pawn = player.PlayerPawn.Value;
                                pawn.ArmorValue = Math.Min(100, pawn.ArmorValue + 20);
                                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                                buffDesc = "🛡护甲";
                            }
                            break;
                    }

                    player.PrintToCenterAlert($"🍀 幸运金币 +${money}! {buffDesc}!");

                    float iv = _interval.TryGetValue(player, out float v) ? v
                        : _config.Dices.Lucky.IntervalMin + (float)_random.NextDouble()
                            * (_config.Dices.Lucky.IntervalMax - _config.Dices.Lucky.IntervalMin);
                    _nextTickTime[player] = now + iv;
                }
                catch
                {
                    _nextTickTime.Remove(player); _interval.Remove(player);
                }
            }
        }
    }
}
