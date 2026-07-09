using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Ragnarok : DiceBlueprint
    {
        public override string ClassName => "Ragnarok";
        private bool _comboActive;
        private static bool _timeAccelerated;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        public override List<string> Events => ["EventPlayerDeath"];
        private float _roundStartTime;
        private bool _roundEnded;
        private CCSPlayerController? _holder; // the player who holds Ragnarok
        private int _holderTeam; // team of holder, survives death for death-trigger
        private bool _holderDied;

        public Ragnarok(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            if (_roundStartTime == 0) _roundStartTime = (float)Server.CurrentTime;
            _roundEnded = false;
            _holder = player;
            _holderTeam = player.TeamNum;
            _holderDied = false;
            _comboActive = DiceSynergy.HasPartner(player, "NukeLeak");
            if (_comboActive && !_timeAccelerated)
            {
                _timeAccelerated = true;
                _roundStartTime += _config.Dices.Ragnarok.RoundDuration * 0.5f;
                DiceSynergy.AnnounceCombo(player, "末日审判", "诸神黄昏+核泄漏！终焉加速降临！");
            }
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏳ 终焉降临！持有者30s无敌，60s后与一名队友共赴黄昏！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset() { _players.Clear(); _roundStartTime = 0; _roundEnded = true; _timeAccelerated = false; _holder = null; _holderDied = false; }
        public override void Destroy() => Reset();

        // Only the holder is invincible
        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0 || _roundEnded || _holder == null) return HookResult.Continue;
            if (_holder.PlayerPawn?.Value == null || !_holder.PlayerPawn.Value.IsValid) return HookResult.Continue;

            // Check if the victim is the holder's pawn
            if (entity.Handle != _holder.PlayerPawn.Value.Handle) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            float invulDuration = _config.Dices.Ragnarok.InvulDuration * (_timeAccelerated ? 0.5f : 1f);
            if (now - _roundStartTime < invulDuration)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }

        // If holder dies before time's up, take a random teammate
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (_roundEnded || _holderDied) return HookResult.Continue;
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid || _holder == null) return HookResult.Continue;
            if (victim != _holder) return HookResult.Continue;

            // Holder died early — take a random teammate
            _holderDied = true;
            _roundEnded = true;

            var teammates = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p != _holder
                    && p.TeamNum == _holderTeam
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            CCSPlayerController? sacrifice = null;
            if (teammates.Count > 0)
                sacrifice = teammates[Random.Shared.Next(teammates.Count)];

            if (sacrifice != null)
            {
                var capS = sacrifice;
                if (!capS.IsBot && !capS.IsHLTV)
                    capS.PlayerPawn!.Value!.CommitSuicide(false, true);
                else
                {
                    try { capS.PlayerPawn!.Value!.CommitSuicide(false, true); }
                    catch
                    {
                        capS.PlayerPawn!.Value!.Health = 0;
                        Utilities.SetStateChanged(capS.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                    }
                }
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💀 {victim.PlayerName} 提前陨落！{sacrifice.PlayerName} 被终焉之力吞噬！");
            }
            else
            {
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💀 {victim.PlayerName} 提前陨落！终焉消逝...");
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0 || _roundEnded) return;
            float now = (float)Server.CurrentTime;
            float elapsed = now - _roundStartTime;
            float total = _config.Dices.Ragnarok.RoundDuration * (_timeAccelerated ? 0.5f : 1f);

            // Countdown warnings (widen windows to avoid missing)
            if (elapsed >= total - 30f && elapsed < total - 29f)
            {
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏳ 终焉还剩30秒！");
                _holder?.PrintToCenterAlert("⏳ 终焉还剩30秒");
            }
            if (elapsed >= total - 20f && elapsed < total - 19f)
            {
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏳ 终焉还剩20秒！");
                _holder?.PrintToCenterAlert("⏳ 终焉还剩20秒");
            }
            if (elapsed >= total - 10f && elapsed < total - 9.9f)
            {
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏰ 终焉还剩10秒！");
                _holder?.PrintToCenterAlert("⏳ 终焉还剩10秒");
            }
            if (elapsed >= total - 5f && elapsed < total - 4.9f)
            {
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💀 终焉还剩5秒！");
                _holder?.PrintToCenterAlert("⏳ 终焉还剩5秒");
            }

            if (elapsed >= total)
            {
                _roundEnded = true;

                // Kill the holder if still alive
                if (_holder?.PlayerPawn?.Value != null && _holder.PlayerPawn.Value.IsValid
                    && _holder.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    if (!_holder.IsBot && !_holder.IsHLTV)
                        _holder.PlayerPawn.Value.CommitSuicide(false, true);
                    else
                    {
                        try { _holder.PlayerPawn.Value.CommitSuicide(false, true); }
                        catch
                        {
                            _holder.PlayerPawn.Value.Health = 0;
                            Utilities.SetStateChanged(_holder.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    }
                }

                // Kill one random alive teammate
                var teammates = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV && p != _holder
                        && p.TeamNum == _holderTeam
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .ToList();

                if (teammates.Count > 0)
                {
                    var sacrifice = teammates[Random.Shared.Next(teammates.Count)];
                    if (!sacrifice.IsBot && !sacrifice.IsHLTV)
                        sacrifice.PlayerPawn!.Value!.CommitSuicide(false, true);
                    else
                    {
                        try { sacrifice.PlayerPawn!.Value!.CommitSuicide(false, true); }
                        catch
                        {
                            sacrifice.PlayerPawn!.Value!.Health = 0;
                            Utilities.SetStateChanged(sacrifice.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    }
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💀 终焉！{sacrifice.PlayerName} 被终焉之力吞噬！");
                }
                else
                {
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}💀 终焉降临！持有者已陨落...");
                }
            }
        }
    }
}
