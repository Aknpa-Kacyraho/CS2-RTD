using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class FourtyTwo : DiceBlueprint
    {
        public override string ClassName => "FourtyTwo";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, float> _nextTrigger = [];
        private readonly Dictionary<CCSPlayerController, float> _invulEnd = [];
        private readonly Dictionary<CCSPlayerController, float> _invisEnd = [];
        private readonly Dictionary<CCSPlayerController, bool> _invisShown = [];

        public FourtyTwo(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _nextTrigger[player] = (float)Server.CurrentTime + _config.Dices.FourtyTwo.Interval;
            _invulEnd[player] = 0f;
            _invisEnd[player] = 0f;
            _invisShown[player] = false;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player); _ = _nextTrigger.Remove(player); _ = _invulEnd.Remove(player); _ = _invisEnd.Remove(player); _ = _invisShown.Remove(player);
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
        }

        public override void Reset() { _players.Clear(); _nextTrigger.Clear(); _invulEnd.Clear(); _invisEnd.Clear(); _invisShown.Clear(); }
        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            if (_invulEnd.TryGetValue(victim, out float invEnd) && now < invEnd)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;

                if (_nextTrigger.TryGetValue(player, out float next) && now >= next)
                {
                    _nextTrigger[player] = now + _config.Dices.FourtyTwo.Interval;
                    _invulEnd[player] = now + _config.Dices.FourtyTwo.InvulDuration;
                    _invisEnd[player] = now + _config.Dices.FourtyTwo.InvulDuration + _config.Dices.FourtyTwo.InvisDuration;
                    _invisShown[player] = false;
                    player.PrintToCenterAlert("4️⃣2️⃣ 无敌4s！");
                }

                if (_invulEnd.TryGetValue(player, out float invEnd) && now >= invEnd
                    && _invisEnd.TryGetValue(player, out float invisTill) && now < invisTill
                    && !_invisShown.GetValueOrDefault(player, false))
                {
                    _invisShown[player] = true;
                    player.PrintToCenterAlert("隐身2s！");
                }

                if (_invisEnd.TryGetValue(player, out float invEnd2) && now < invEnd2)
                {
                    float invulEnd = _invulEnd.GetValueOrDefault(player, now);
                    if (now < invulEnd)
                    {
                        pawn.Render = Color.FromArgb(100, 255, 255, 255);
                    }
                    else
                    {
                        pawn.Render = Color.FromArgb(10, 255, 255, 255);
                    }
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                }
                else if (pawn.Render != Color.FromArgb(255, 255, 255, 255))
                {
                    pawn.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
    }
}
