using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Cthulhu : DiceBlueprint
    {
        public override string ClassName => "Cthulhu";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick"];

        /// <summary>Static round start time so countdown survives Cthulhu player death.</summary>
        private static float _roundStartTime;
        /// <summary>Static team number remembered so kill targets correct team even after Cthulhu dies.</summary>
        private static int _cthulhuTeam;
        private readonly Dictionary<ulong, float> _enemySpeedReduction = [];
        private int _lastWarningSecond = -1;

        public Cthulhu(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.MaxHealth = 1;
            pawn.Health = 1;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            MoveLockManager.Lock(player, "Cthulhu");

            _players.Add(player);
            if (_roundStartTime == 0) _roundStartTime = (float)Server.CurrentTime;
            _cthulhuTeam = player.TeamNum;
            _lastWarningSecond = -1;

            _comboActive = DiceSynergy.HasPartner(player, "DuskDawn");
            if (_comboActive)
            {
                DiceSynergy.AnnounceCombo(player, "深渊觉醒", "克苏恩+暮光！深渊觉醒了！");
                DuskDawn.TriggerDawn(player);
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🐙 克苏恩降临！{_config.Dices.Cthulhu.KillTime:F0}秒后吞噬一切！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            MoveLockManager.Unlock(player, "Cthulhu");
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) MoveLockManager.Unlock(p, "Cthulhu");
            _players.Clear();
            _roundStartTime = 0;
            _cthulhuTeam = 0;
            _enemySpeedReduction.Clear();
            _lastWarningSecond = -1;
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            // Countdown survives player death — use static _roundStartTime
            if (_roundStartTime == 0) return;

            float now = (float)Server.CurrentTime;
            float elapsed = now - _roundStartTime;
            float killTime = _config.Dices.Cthulhu.KillTime;

            // Keep living Cthulhu players locked
            foreach (var player in _players.ToList())
            {
                if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                    MoveLockManager.Lock(player, "Cthulhu");

                // Combo: regen 1HP/sec for owner
                if (_comboActive && Server.TickCount % 64 == 0
                    && player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    var p = player.PlayerPawn.Value;
                    p.Health = Math.Min(p.Health + 1, p.MaxHealth);
                    Utilities.SetStateChanged(p, "CBaseEntity", "m_iHealth");
                }
            }

            // Warnings: trigger per-second rather than narrow time windows
            int remainingSec = (int)Math.Ceiling(killTime - elapsed);
            if (remainingSec != _lastWarningSecond)
            {
                _lastWarningSecond = remainingSec;
                if (remainingSec == 30 || remainingSec == 10 || remainingSec == 5)
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🐙 克苏恩还剩{remainingSec}秒！");
            }

            // Speed drain on enemies (only while Cthulhu player is alive)
            if (_players.Count > 0)
            {
                foreach (var enemy in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    if (enemy.TeamNum == _cthulhuTeam) continue;

                    CCSPlayerPawn ep = enemy.PlayerPawn!.Value!;
                    ulong sid = enemy.SteamID;
                    float currentReduction = _enemySpeedReduction.GetValueOrDefault(sid, 0f);
                    float newReduction = currentReduction + _config.Dices.Cthulhu.SpeedLossPerSec * Server.TickInterval;
                    if (newReduction > 1.0f) newReduction = 1.0f;
                    _enemySpeedReduction[sid] = newReduction;

                    ep.VelocityModifier = 1.0f - newReduction;
                    Utilities.SetStateChanged(ep, "CCSPlayerPawn", "m_flVelocityModifier");

                    int hpLoss = _config.Dices.Cthulhu.HpLossPerSec * (_comboActive ? 2 : 1);
                    if (Server.TickCount % 64 == 0)
                    {
                        ep.Health -= hpLoss;
                        Utilities.SetStateChanged(ep, "CBaseEntity", "m_iHealth");
                        if (ep.Health <= 0)
                        {
                            if (!enemy.IsBot && !enemy.IsHLTV)
                                ep.CommitSuicide(false, true);
                            else
                            {
                                try { ep.CommitSuicide(false, true); }
                                catch
                                {
                                    ep.Health = 0;
                                    Utilities.SetStateChanged(ep, "CBaseEntity", "m_iHealth");
                                }
                            }
                        }
                    }
                }
            }

            // Kill time reached — kill all enemies and broadcast
            if (elapsed >= killTime)
            {
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🐙 克苏恩吞噬万物！所有敌人被吞噬！");

                foreach (var p in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    // Kill everyone except dead Cthulhu player's team (if Cthulhu alive, kill enemies only)
                    if (p.TeamNum == _cthulhuTeam)
                        continue;

                    if (!p.IsBot && !p.IsHLTV)
                        p.PlayerPawn!.Value!.CommitSuicide(false, true);
                    else
                    {
                        try { p.PlayerPawn!.Value!.CommitSuicide(false, true); }
                        catch
                        {
                            p.PlayerPawn!.Value!.Health = 0;
                            Utilities.SetStateChanged(p.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    }
                }

                // One-shot: clear round start time to prevent re-triggering
                _roundStartTime = 0;
            }
        }
    }
}
