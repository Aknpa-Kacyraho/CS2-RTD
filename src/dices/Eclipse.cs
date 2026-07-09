using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Eclipse : DiceBlueprint
    {
        public override string ClassName => "Eclipse";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, float> _nextPhaseTime = [];
        // true = New Moon (fast+stealth), false = Full Moon (slow+hurt)
        private readonly Dictionary<CCSPlayerController, bool> _isNewMoon = [];

        public Eclipse(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _isNewMoon[player] = true;
            _nextPhaseTime[player] = (float)Server.CurrentTime + _config.Dices.Eclipse.NewMoonDuration;
            ApplyPhase(player, true);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🌑 新月降临！速度×1.2，半透明，伤害×0.8...");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            Revert(player);
            _ = _players.Remove(player);
            _ = _nextPhaseTime.Remove(player);
            _ = _isNewMoon.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Revert(p);
            _players.Clear(); _nextPhaseTime.Clear(); _isNewMoon.Clear();
        }

        private void Revert(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
        }

        private void ApplyPhase(CCSPlayerController player, bool newMoon)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            if (newMoon)
            {
                pawn.VelocityModifier = _config.Dices.Eclipse.NewMoonSpeed;
                pawn.Render = Color.FromArgb(100, 180, 180, 220);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
            else
            {
                pawn.VelocityModifier = _config.Dices.Eclipse.FullMoonSpeed;
                pawn.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                    if (!_nextPhaseTime.TryGetValue(player, out float next)) continue;
                    if (now < next) continue;
                    if (!_isNewMoon.TryGetValue(player, out bool isNew)) continue;

                    bool nextPhase = !isNew;
                    _isNewMoon[player] = nextPhase;

                    if (nextPhase)
                    {
                        _nextPhaseTime[player] = now + _config.Dices.Eclipse.NewMoonDuration;
                        ApplyPhase(player, true);
                        player.PrintToCenterAlert("🌑 新月！速度×1.2 半透明 伤害×0.8");
                    }
                    else
                    {
                        _nextPhaseTime[player] = now + _config.Dices.Eclipse.FullMoonDuration;
                        ApplyPhase(player, false);
                        player.PrintToCenterAlert("🌕 满月！速度×0.8 伤害×1.5");
                    }
                }
                catch { }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (!_isNewMoon.TryGetValue(attacker, out bool isNew)) return HookResult.Continue;

            if (isNew)
            {
                info.Damage *= _config.Dices.Eclipse.NewMoonDamageMult;
            }
            else
            {
                info.Damage *= _config.Dices.Eclipse.FullMoonDamageMult;
            }
            return HookResult.Changed;
        }
    }
}
