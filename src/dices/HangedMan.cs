using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class HangedMan : DiceBlueprint
    {
        public override string ClassName => "HangedMan";
        public override List<string> Listeners => ["OnTick"];
        public override List<string> Events => ["EventPlayerDeath"];
        private readonly Dictionary<CCSPlayerController, float> _nextTickTime = [];
        private readonly Dictionary<CCSPlayerController, bool> _reversed = [];

        public HangedMan(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);
            _nextTickTime[player] = 0f;
            _reversed[player] = false;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("💀 命悬一线！每秒扣血，击杀敌人逆转诅咒！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextTickTime.Remove(player);
            _ = _reversed.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _nextTickTime.Clear();
            _reversed.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (attacker == @event.Userid) return HookResult.Continue;
            if (_reversed.TryGetValue(attacker, out bool rev) && rev) return HookResult.Continue;

            _reversed[attacker] = true;
            attacker.PrintToCenterAlert("🔓 诅咒逆转！每秒恢复HP！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_HangedMan_broken"].Value.Replace("{playerName}", attacker.PlayerName)}");
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_nextTickTime.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    if (!_nextTickTime.TryGetValue(player, out float nextTime) || nextTime > now) continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    bool reversed = _reversed.TryGetValue(player, out bool r) && r;

                    if (reversed)
                    {
                        int healHp = _config.Dices.HangedMan.HealHp;
                        int newHp = Math.Min(pawn.Health + healHp, pawn.MaxHealth);
                        if (newHp > pawn.Health)
                        {
                            pawn.Health = newHp;
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        }
                    }
                    else
                    {
                        int drainHp = _config.Dices.HangedMan.DrainHp;
                        pawn.Health -= drainHp;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        if (pawn.Health <= 0)
                        {
                            if (!player.IsBot)
                                pawn.CommitSuicide(false, true);
                        }
                    }

                    _nextTickTime[player] = now + 1.0f;
                }
                catch { _nextTickTime.Remove(player); }
            }
        }
    }
}
