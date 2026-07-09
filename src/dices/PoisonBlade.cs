using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class PoisonBlade : DiceBlueprint
    {
        public override string ClassName => "PoisonBlade";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerHurt"
        ];
        public override List<string> Listeners => [
            "OnTick"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        // (victim, (remainingTicks, damagePerTick, nextTickTime, attacker))
        private readonly Dictionary<CCSPlayerController, (int Ticks, int Dmg, float NextTime)> _poisoned = [];

        public PoisonBlade(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _comboActive = DiceSynergy.HasPartner(player, "IceBeam");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "霜毒双刃", "霜毒双刃联动生效！");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _poisoned.Clear();
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null
                || !attacker.IsValid
                || victim == null
                || !victim.IsValid
                || attacker == victim
                || !_players.Contains(attacker)
                || victim.PlayerPawn?.Value == null
                || !victim.PlayerPawn.Value.IsValid)
            {
                return HookResult.Continue;
            }

            float chance = _config.Dices.PoisonBlade.ChanceMin +
                (float)_random.NextDouble() * (_config.Dices.PoisonBlade.ChanceMax - _config.Dices.PoisonBlade.ChanceMin);

            if (_random.NextDouble() >= chance)
            {
                return HookResult.Continue;
            }

            int dmgPerTick = _random.Next(_config.Dices.PoisonBlade.DamagePerTickMin, _config.Dices.PoisonBlade.DamagePerTickMax + 1);
            int ticks = _comboActive ? _config.Dices.PoisonBlade.TickCount + 3 : _config.Dices.PoisonBlade.TickCount;
            float interval = _config.Dices.PoisonBlade.TickInterval;

            _poisoned[victim] = (ticks, dmgPerTick, (float)Server.CurrentTime);
            victim.PrintToCenterAlert($"☠ 中毒! {ticks * dmgPerTick} 伤害 ({dmgPerTick}x{ticks})!");

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_poisoned.Count == 0) return;

            float now = (float)Server.CurrentTime;
            List<CCSPlayerController> toRemove = [];

            foreach (var (victim, data) in _poisoned)
            {
                if (victim == null
                    || !victim.IsValid
                    || victim.PlayerPawn?.Value == null
                    || !victim.PlayerPawn.Value.IsValid
                    || victim.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    toRemove.Add(victim);
                    continue;
                }

                if (now < data.NextTime) continue;

                var (ticks, dmg, nextTime) = data;
                CCSPlayerPawn pawn = victim.PlayerPawn.Value;
                pawn.Health -= dmg;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                if (ticks <= 1 || pawn.Health <= 0)
                {
                    if (pawn.Health <= 0)
                    {
                        if (!victim.IsBot && !victim.IsHLTV)
                            pawn.CommitSuicide(false, true);
                        else
                        {
                            try { pawn.CommitSuicide(false, true); }
                            catch
                            {
                                pawn.Health = 0;
                                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                            }
                        }
                    }
                    toRemove.Add(victim);
                }
                else
                {
                    _poisoned[victim] = (ticks - 1, dmg, now + _config.Dices.PoisonBlade.TickInterval);
                }
            }

            foreach (var v in toRemove)
            {
                _ = _poisoned.Remove(v);
            }
        }
    }
}
