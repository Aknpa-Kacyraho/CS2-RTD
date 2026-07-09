using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Jester : DiceBlueprint
    {
        public override string ClassName => "Jester";
        public override List<string> Listeners => [
            "OnTick"
        ];
        public override List<string> Events => ["EventPlayerDeath"];
        private readonly Dictionary<CCSPlayerController, float> _nextDamageTime = [];
        private readonly HashSet<CCSPlayerController> _hasKill = [];
        public Jester(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _nextDamageTime[player] = 0f;

            SpeedBonusManager.Register(player, "Jester", _config.Dices.Jester.SpeedMultiplier - 1.0f);

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextDamageTime.Remove(player);
            _ = _hasKill.Remove(player);
            SpeedBonusManager.Unregister(player, "Jester");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                SpeedBonusManager.Unregister(p, "Jester");
            _players.Clear();
            _nextDamageTime.Clear();
            _hasKill.Clear();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            _hasKill.Add(attacker);
            attacker.PrintToCenterAlert("🤡 小丑笑了！移动不再扣血，改为回血！");
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    if (!_nextDamageTime.TryGetValue(player, out float next) || next > now)
                        continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;

                    float speed = MathF.Sqrt(
                        (pawn.AbsVelocity.X * pawn.AbsVelocity.X) +
                        (pawn.AbsVelocity.Y * pawn.AbsVelocity.Y) +
                        (pawn.AbsVelocity.Z * pawn.AbsVelocity.Z));

                    if (speed > 10f)
                    {
                        int currentHealth = pawn.Health;
                        if (_hasKill.Contains(player))
                        {
                            int newHealth = Math.Min(currentHealth + _config.Dices.Jester.DamagePerSecond, pawn.MaxHealth);
                            pawn.Health = newHealth;
                        }
                        else
                        {
                            int newHealth = currentHealth - _config.Dices.Jester.DamagePerSecond;
                            if (newHealth <= 0)
                            {
                                if (!player.IsBot)
                                    pawn.CommitSuicide(false, true);
                                continue;
                            }
                            pawn.Health = newHealth;
                        }
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }

                    // Always apply speed boost
                    float effective = SpeedBonusManager.GetEffective(player);
                    pawn.VelocityModifier = 1 + effective;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

                    _nextDamageTime[player] = now + 1f;
                }
                catch { }
            }
        }
    }
}
