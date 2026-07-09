using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Fool : DiceBlueprint
    {
        public override string ClassName => "Fool";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private readonly Dictionary<ulong, float> _invulEndTime = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Fool(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _invulEndTime.Remove(player.SteamID);
        }

        public override void Reset()
        {
            foreach (var sid in _invulEndTime.Keys.ToList())
            {
                var p = Utilities.GetPlayers().FirstOrDefault(x => x.SteamID == sid);
                if (p?.PlayerPawn?.Value is CCSPlayerPawn pawn && pawn.IsValid)
                    pawn.TakesDamage = true;
            }
            _players.Clear();
            _invulEndTime.Clear();
        }

        // Check/clear expired invul on any damage event (covers both attacker and victim role)
        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();

            float now = (float)Server.CurrentTime;

            // --- Attacker is a Fool: 50% chance attack whiffs (deals 0 damage) ---
            if (attacker != null && attacker.IsValid && _players.Contains(attacker))
            {
                if (_random.NextDouble() < _config.Dices.Fool.AttackWhiffChance)
                {
                    info.Damage = 0;
                }
            }

            // --- Victim is a Fool: 50% chance gain 2s invincibility when hit ---
            if (victim != null && victim.IsValid && _players.Contains(victim) && info.Damage > 0)
            {
                // Check/clear expired invul
                if (_invulEndTime.TryGetValue(victim.SteamID, out float invulEnd) && now >= invulEnd)
                {
                    if (victim.PlayerPawn?.Value is CCSPlayerPawn vp && vp.IsValid)
                        vp.TakesDamage = true;
                    _invulEndTime.Remove(victim.SteamID);
                }

                // Not currently invul → roll for new invul
                if (!_invulEndTime.ContainsKey(victim.SteamID)
                    && _random.NextDouble() < _config.Dices.Fool.InvincibilityChance)
                {
                    if (victim.PlayerPawn?.Value is CCSPlayerPawn vp && vp.IsValid)
                    {
                        vp.TakesDamage = false;
                        float duration = _config.Dices.Fool.InvincibilitySeconds;
                        _invulEndTime[victim.SteamID] = now + duration;
                        victim.PrintToCenterAlert($"🃏 愚者庇护！无敌{duration:F0}秒！");

                        CCSPlayerController capV = victim;
                        new CounterStrikeSharp.API.Modules.Timers.Timer(duration, () =>
                        {
                            if (capV?.PlayerPawn?.Value is CCSPlayerPawn pp && pp.IsValid)
                                pp.TakesDamage = true;
                            _invulEndTime.Remove(capV.SteamID);
                        });
                    }
                }

                // If currently invul, block damage
                if (_invulEndTime.ContainsKey(victim.SteamID))
                {
                    info.Damage = 0;
                }
            }

            return info.Damage == 0 ? HookResult.Changed : HookResult.Continue;
        }
    }
}
