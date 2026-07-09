using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Gaia : DiceBlueprint
    {
        public override string ClassName => "Gaia";
        public override List<string> Listeners => ["OnTick"];
        private readonly Dictionary<CCSPlayerController, float> _nextHealTime = [];

        public Gaia(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _nextHealTime[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player); _ = _nextHealTime.Remove(player);
        }

        public override void Reset() { _players.Clear(); _nextHealTime.Clear(); }
        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                if (!_nextHealTime.TryGetValue(player, out float next) || now >= next)
                {
                    _nextHealTime[player] = now + 1f;
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    int cap = _config.Dices.Gaia.MaxHP;
                    pawn.MaxHealth = Math.Max(pawn.MaxHealth, Math.Min(pawn.Health + _config.Dices.Gaia.HpPerSecond, cap));
                    pawn.Health = Math.Min(pawn.Health + _config.Dices.Gaia.HpPerSecond, cap);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }
        }
    }
}
