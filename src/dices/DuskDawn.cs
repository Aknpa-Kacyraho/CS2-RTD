using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DuskDawn : DiceBlueprint
    {
        public override string ClassName => "DuskDawn";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private readonly Dictionary<CCSPlayerController, bool> _triggered = [];
        private readonly Dictionary<CCSPlayerController, float> _lastHealTime = [];
        private static readonly HashSet<ulong> _forceTriggered = [];

        public static void TriggerDawn(CCSPlayerController player) => _forceTriggered.Add(player.SteamID);

        public DuskDawn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
            : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            bool forced = _forceTriggered.Contains(player.SteamID);
            _triggered[player] = forced;
            _lastHealTime[player] = 0f;

            _comboActive = DiceSynergy.HasPartner(player, "Cthulhu");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "深渊觉醒", "克苏恩+暮光！深渊觉醒了！");

            if (forced)
            {
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                pawn.MaxHealth = _config.Dices.DuskDawn.MaxHealth;
                pawn.Health = Math.Min(pawn.Health, pawn.MaxHealth);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                player.PrintToCenterAlert("☀️ 深渊觉醒！破晓已触发！");
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☀️ {player.PlayerName} 深渊觉醒！破晓降临！");
                return;
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🌅 暮光：命悬一线时将触发破晓！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _triggered.Remove(player);
            _ = _lastHealTime.Remove(player);
            _forceTriggered.Remove(player.SteamID);
        }

        public override void Reset()
        {
            _players.Clear();
            _triggered.Clear();
            _lastHealTime.Clear();
            _forceTriggered.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            if (info.Damage <= 0) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;
            if (_triggered.TryGetValue(victim, out bool done) && done)
                return HookResult.Continue;

            CCSPlayerPawn pawn = victim.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;

            int healthAfter = pawn.Health - (int)float.Round(info.Damage);
            if (healthAfter != 1) return HookResult.Continue;

            _triggered[victim] = true;
            info.Damage = pawn.Health - 1;

            pawn.MaxHealth = _config.Dices.DuskDawn.MaxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            victim.PrintToCenterAlert("☀️ 破晓！每秒回复100HP+10护甲，上限150HP！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☀️ {victim.PlayerName} 触发破晓！每秒回复100HP+10护甲！");

            return HookResult.Changed;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (!_triggered.TryGetValue(player, out bool done) || !done) continue;
                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;

                if (!_lastHealTime.TryGetValue(player, out float last) || now - last >= 1f)
                {
                    _lastHealTime[player] = now;

                    int maxHp = _config.Dices.DuskDawn.MaxHealth;
                    int heal = _config.Dices.DuskDawn.HealPerSec;
                    int armor = _config.Dices.DuskDawn.ArmorPerSec;

                    if (pawn.Health < maxHp)
                    {
                        pawn.Health = Math.Min(pawn.Health + heal, maxHp);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                    if (pawn.ArmorValue < 100)
                    {
                        pawn.ArmorValue = Math.Min(pawn.ArmorValue + armor, 100);
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                    }

                    player.PrintToCenterAlert($"☀️ 破晓 {pawn.Health}/{maxHp}HP +{armor}甲/s");
                }
            }
        }
    }
}
