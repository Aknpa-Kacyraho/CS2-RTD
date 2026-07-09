using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Regeneration : DiceBlueprint
    {
        public override string ClassName => "Regeneration";
        public override List<string> Listeners => ["OnTick"];
        private readonly Dictionary<CCSPlayerController, float> _nextHealTime = [];
        private bool _comboActive;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Regeneration(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid)
                return;
            _players.Add(player);
            _nextHealTime[player] = 0;

            _comboActive = DiceSynergy.HasPartner(player, "JumpHeal");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "生命律动", "生命之泉3~5HP/s回血翻倍(6~10HP/s)！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextHealTime.Remove(player);
        }

        public override void Reset() { _players.Clear(); _nextHealTime.Clear(); }
        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_nextHealTime.Count == 0) return;
            float now = (float)Server.CurrentTime;
            float interval = 1.0f;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    if (!_nextHealTime.TryGetValue(player, out float nextTime) || nextTime > now)
                        continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    int healAmount = _random.Next(3, 6);
                    if (_comboActive) healAmount *= 2;
                    int newHealth = Math.Min(pawn.Health + healAmount, pawn.MaxHealth);
                    if (newHealth > pawn.Health)
                    {
                        pawn.Health = newHealth;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                    _nextHealTime[player] = now + interval;
                }
                catch { _nextHealTime.Remove(player); }
            }
        }
    }
}
