using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Linq;

namespace RollTheDice.Dices
{
    public class Bugle : DiceBlueprint
    {
        public override string ClassName => "Bugle";
        private bool _comboActive;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private float _startTime;
        private bool _expired;

        public Bugle(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            if (_startTime == 0)
                _startTime = (float)Server.CurrentTime;

            _expired = false;

            foreach (var t in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum == player.TeamNum
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                SpeedBonusManager.Register(t, "Bugle", _config.Dices.Bugle.SpeedMultiplier - 1.0f);
                DamageBonusManager.Register(t, "Bugle", _config.Dices.Bugle.DamageMultiplier - 1.0f);
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Bugle_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");

            _comboActive = DiceSynergy.HasPartner(player, "World");
            if (_comboActive)
            {
                DiceSynergy.AnnounceCombo(player, "天启", "冲锋号+世界！天启降临！");
                // Give 2 random alive teammates an extra dice
                var pool = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV && p != player
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .OrderBy(_ => _random.Next())
                    .Take(2)
                    .ToList();
                var instance = RollTheDice.Instance;
                foreach (var t in pool)
                {
                    instance?.ForceDiceForPlayer(t);
                }
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (_players.Count == 0)
                ClearAllBuffs();
        }

        public override void Reset()
        {
            ClearAllBuffs();
            _players.Clear();
            _startTime = 0;
            _expired = true;
        }

        public override void Destroy() => Reset();

        private void ClearAllBuffs()
        {
            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid))
            {
                SpeedBonusManager.Unregister(p, "Bugle");
                DamageBonusManager.Unregister(p, "Bugle");
            }
        }

        public void OnTick()
        {
            if (_players.Count == 0 || _expired) return;
            float now = (float)Server.CurrentTime;
            float elapsed = now - _startTime;
            float remaining = _config.Dices.Bugle.Duration - elapsed;

            if (remaining <= 10f && remaining > 9.7f)
            {
                foreach (var player in _players.ToList())
                    player?.PrintToCenterAlert("📯 冲锋号还剩10秒！");
            }
            if (remaining <= 5f && remaining > 4.7f)
            {
                foreach (var player in _players.ToList())
                    player?.PrintToCenterAlert("📯 冲锋号还剩5秒！");
            }

            if (elapsed >= _config.Dices.Bugle.Duration)
            {
                _expired = true;
                ClearAllBuffs();

                foreach (var player in _players.ToList())
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    float speedBonus = SpeedBonusManager.GetEffective(player);
                    player.PlayerPawn.Value.VelocityModifier = 1 + speedBonus;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }

                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}📯 冲锋号结束！全员效果消退！");
                return;
            }

            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                if (!SpeedBonusManager.HasAny(p)) continue;
                float effective = SpeedBonusManager.GetEffective(p);
                p.PlayerPawn!.Value!.VelocityModifier = 1 + effective;
                Utilities.SetStateChanged(p.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid || _expired) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid) return HookResult.Continue;

            if (SpeedBonusManager.HasAny(attacker) && _players.Any(p => p.TeamNum == attacker.TeamNum))
            {
                if (DamageBonusManager.IsHighest(attacker, "Bugle"))
                {
                    float effective = DamageBonusManager.GetEffective(attacker);
                    if (effective > 0)
                    {
                        info.Damage = (int)(info.Damage * (1 + effective));
                        return HookResult.Changed;
                    }
                }
            }

            return HookResult.Continue;
        }
    }
}
