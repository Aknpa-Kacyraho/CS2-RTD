using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class NukeLeak : DiceBlueprint
    {
        public override string ClassName => "NukeLeak";
        private bool _comboActive;
        private static bool _timeAccelerated;
        public override List<string> Listeners => ["OnTick"];
        private float _detonationTime;
        private bool _detonated;

        public NukeLeak(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            if (_detonationTime == 0)
            {
                _comboActive = DiceSynergy.HasPartner(player, "Ragnarok");
                float seconds = _comboActive ? _config.Dices.NukeLeak.DetonationSeconds * 0.5f : _config.Dices.NukeLeak.DetonationSeconds;
                _detonationTime = (float)Server.CurrentTime + seconds;
                _detonated = false;
                if (_comboActive)
                {
                    _timeAccelerated = true;
                    DiceSynergy.AnnounceCombo(player, "末日审判", "诸神黄昏+核泄漏！终焉加速降临！");
                }
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_NukeLeak_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            // Do NOT reset the timer — the nuke persists even if the dice owner dies
        }

        public override void Reset() { _players.Clear(); _detonationTime = 0; _detonated = false; _timeAccelerated = false; }

        public void OnTick()
        {
            if (_detonationTime == 0 || _detonated) return;
            float now = (float)Server.CurrentTime;
            float remaining = _detonationTime - now;

            // Countdown broadcasts
            if (remaining <= 0)
            {
                _detonated = true;
                // Kill all alive players (including bots)
                foreach (var player in Utilities.GetPlayers())
                {
                    if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                    if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    if (!player.IsBot && !player.IsHLTV)
                        player.PlayerPawn.Value.CommitSuicide(false, true);
                    else
                    {
                        try { player.PlayerPawn.Value.CommitSuicide(false, true); }
                        catch
                        {
                            player.PlayerPawn.Value.Health = 0;
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    }
                }
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_NukeLeak_detonated"].Value}");
                return;
            }

            // Tick every ~1s for countdown display
            if (Server.TickCount % 64 == 0)
            {
                int sec = (int)Math.Ceiling(remaining);
                if (sec == 60 || sec == 30 || sec == 15 || sec <= 10)
                {
                    foreach (var player in _players)
                        player?.PrintToCenterAlert($"☢ 核弹泄露！{sec}s");
                }
            }
        }
    }
}
