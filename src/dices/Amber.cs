using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Amber : DiceBlueprint
    {
        public override string ClassName => "Amber";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre", "OnTick"];
        private readonly Dictionary<ulong, float> _frozenUntil = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Amber(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "IceBeam");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "极寒地狱", "冻结时间翻倍+琥珀概率翻倍！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        { _ = _players.Remove(player); }

        public override void Reset() { _players.Clear(); _frozenUntil.Clear(); }
        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            // Block attacks from frozen players
            if (_frozenUntil.TryGetValue(victim.SteamID, out float endTime)
                && (float)Server.CurrentTime < endTime)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || attacker == victim) return HookResult.Continue;

            // Block attacks from frozen attackers
            if (_frozenUntil.TryGetValue(attacker.SteamID, out float attackerEnd)
                && (float)Server.CurrentTime < attackerEnd)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            float chance = (float)(_random.NextDouble() * (_config.Dices.Amber.FreezeChanceMax - _config.Dices.Amber.FreezeChanceMin) + _config.Dices.Amber.FreezeChanceMin) * (_comboActive ? 2f : 1f);
            if (_random.NextDouble() < chance)
            {
                float now = (float)Server.CurrentTime;
                float duration = _config.Dices.Amber.FreezeDuration;
                float existingEnd = _frozenUntil.GetValueOrDefault(attacker.SteamID, 0f);
                float newEnd = Math.Max(now, existingEnd) + duration;
                _frozenUntil[attacker.SteamID] = newEnd;
                attacker.PrintToCenterAlert("🟡 被琥珀冻结！无法移动和攻击！");
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_frozenUntil.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var (steamId, endTime) in _frozenUntil.ToList())
            {
                if (now >= endTime)
                {
                    _frozenUntil.Remove(steamId);
                    var p = Utilities.GetPlayers().FirstOrDefault(x => x.SteamID == steamId);
                    if (p?.PlayerPawn?.Value is CCSPlayerPawn paw && paw.IsValid)
                    {
                        paw.MoveType = MoveType_t.MOVETYPE_WALK;
                        Schema.SetSchemaValue(paw.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_WALK);
                        paw.Render = Color.FromArgb(255, 255, 255, 255);
                        Utilities.SetStateChanged(paw, "CBaseModelEntity", "m_clrRender");
                    }
                    continue;
                }

                var player = Utilities.GetPlayers().FirstOrDefault(x => x.SteamID == steamId);
                if (player?.PlayerPawn?.Value is CCSPlayerPawn pawn && pawn.IsValid)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_NONE;
                    Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NONE);
                    pawn.Render = Color.FromArgb(255, 255, 200, 50);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
    }
}
