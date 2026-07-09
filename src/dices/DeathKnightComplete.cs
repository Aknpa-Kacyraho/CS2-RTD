using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DeathKnightComplete : DiceBlueprint
    {
        public override string ClassName => "DeathKnightComplete";
        public override bool CanBeDrawn => false;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        public override List<string> Events => ["EventPlayerDeath"];

        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];
        private readonly Dictionary<CCSPlayerController, int> _originalArmor = [];
        private readonly Dictionary<CCSPlayerController, float> _lastHealTime = [];
        private readonly Dictionary<CCSPlayerController, int> _startMaxHP = [];
        public static readonly HashSet<ulong> DeniedNextRound = [];

        public DeathKnightComplete(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            _originalMaxHealth[player] = pawn.MaxHealth;
            _originalArmor[player] = pawn.ArmorValue;

            pawn.MaxHealth = _config.Dices.DeathKnightComplete.BonusHP;
            pawn.Health = _config.Dices.DeathKnightComplete.BonusHP;
            pawn.ArmorValue = _config.Dices.DeathKnightComplete.BonusArmor;
            _startMaxHP[player] = pawn.MaxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            _players.Add(player);
            _lastHealTime[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DamageReductionManager.Unregister(player, "DeathKnightComplete");
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                if (_originalMaxHealth.TryGetValue(player, out int origMax))
                {
                    pawn.MaxHealth = origMax;
                    pawn.Health = Math.Min(pawn.Health, origMax);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    _originalMaxHealth.Remove(player);
                }
                if (_originalArmor.TryGetValue(player, out int origArmor))
                {
                    pawn.ArmorValue = Math.Min(pawn.ArmorValue, origArmor);
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                    _originalArmor.Remove(player);
                }
            }
            _ = _players.Remove(player);
            _ = _lastHealTime.Remove(player);
            _ = _startMaxHP.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Remove(p);
            _players.Clear();
            _originalMaxHealth.Clear();
            _originalArmor.Clear();
            _lastHealTime.Clear();
            _startMaxHP.Clear();
            DeniedNextRound.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            if (!_startMaxHP.TryGetValue(victim, out int startHP) || startHP <= 0)
                return HookResult.Continue;

            CCSPlayerPawn pawn = victim.PlayerPawn!.Value!;
            int hpLost = startHP - pawn.Health;
            if (hpLost < 0) hpLost = 0;
            float baseReduction = _config.Dices.DeathKnightComplete.InitialDamageReduction;
            float hpRatio = (float)hpLost / startHP;
            float reduction = baseReduction + hpRatio * (1 - baseReduction);
            float maxReduce = _config.Dices.DeathKnightComplete.MaxDamageReduction;
            if (reduction > maxReduce) reduction = maxReduce;

            info.Damage = (int)(info.Damage * (1 - reduction));
            return HookResult.Changed;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                bool hasKnife = false;
                if (player.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value?.DesignerName is string wn && wn.Contains("knife"))
                    hasKnife = true;

                if (!hasKnife) continue;

                if (_lastHealTime.TryGetValue(player, out float lastHeal) && now - lastHeal >= 1.0f)
                {
                    _lastHealTime[player] = now;
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    pawn.Health = Math.Min(pawn.Health + _config.Dices.DeathKnightComplete.KnifeHealPerSec, pawn.MaxHealth);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
                return HookResult.Continue;
            if (!_players.Contains(attacker)) return HookResult.Continue;

            if (attacker.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value?.DesignerName is string wn && wn.Contains("knife"))
            {
                DeniedNextRound.Add(victim.SteamID);
                victim.PrintToCenterAlert("☠ 被死亡骑士的霜之哀伤斩杀！下回合无法获得骰子！");
            }

            return HookResult.Continue;
        }
    }
}
