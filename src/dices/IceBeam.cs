using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class IceBeam : DiceBlueprint
    {
        public override string ClassName => "IceBeam";
        private bool _comboActive;
        private bool _amberCombo;
        public override List<string> Events => ["EventPlayerHurt"];
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer> _frozenPlayers = [];
        private readonly HashSet<ulong> _frozenSteamIDs = [];

        public IceBeam(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "PoisonBlade") || DiceSynergy.HasPartner(player, "Amber");
            if (DiceSynergy.HasPartner(player, "PoisonBlade"))
                DiceSynergy.AnnounceCombo(player, "霜毒双刃", "霜毒双刃联动生效！");
            if (DiceSynergy.HasPartner(player, "Amber"))
            {
                DiceSynergy.AnnounceCombo(player, "极寒地狱", "冻结时间翻倍+琥珀概率翻倍！");
                _amberCombo = true;
            }
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName }, { "chance", (_config.Dices.IceBeam.FreezeChance * 100).ToString("F0") } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        { _ = _players.Remove(player); }

        public override void Reset()
        {
            _players.Clear();
            foreach (var timer in _frozenPlayers.Values) timer?.Kill();
            _frozenPlayers.Clear();
            _frozenSteamIDs.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker != null && attacker.IsValid && _frozenSteamIDs.Contains(attacker.SteamID))
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid
                || !_players.Contains(attacker) || victim.Pawn?.Value == null || !victim.Pawn.Value.IsValid)
                return HookResult.Continue;

            if (_random.NextDouble() >= _config.Dices.IceBeam.FreezeChance)
                return HookResult.Continue;

            if (_frozenPlayers.ContainsKey(victim))
                _frozenPlayers[victim]?.Kill();

            _frozenSteamIDs.Add(victim.SteamID);
            CCSPlayerPawn victimPawn = victim.PlayerPawn.Value;
            victimPawn.VelocityModifier = _config.Dices.IceBeam.SlowAmount;
            Utilities.SetStateChanged(victimPawn, "CCSPlayerPawn", "m_flVelocityModifier");
            victimPawn.Render = Color.FromArgb(255, 0, 255, 255);
            Utilities.SetStateChanged(victimPawn, "CBaseModelEntity", "m_clrRender");
            MoveLockManager.Lock(victim, "IceBeam");

            victim.PrintToCenterAlert("❄ 冻结！无法移动和攻击！");

            CCSPlayerController capturedVictim = victim;
            _frozenPlayers[victim] = new CounterStrikeSharp.API.Modules.Timers.Timer(
                _amberCombo ? _config.Dices.IceBeam.FreezeDuration * 2f : _comboActive ? _config.Dices.IceBeam.FreezeDuration * 1.5f : _config.Dices.IceBeam.FreezeDuration,
                () =>
                {
                    MoveLockManager.Unlock(capturedVictim, "IceBeam");
                    _frozenSteamIDs.Remove(capturedVictim.SteamID);
                    if (capturedVictim?.PlayerPawn?.Value != null && capturedVictim.PlayerPawn.Value.IsValid)
                    {
                        CCSPlayerPawn p = capturedVictim.PlayerPawn.Value;
                        p.VelocityModifier = 1.0f;
                        p.Render = Color.FromArgb(255, 255, 255, 255);
                        Utilities.SetStateChanged(p, "CCSPlayerPawn", "m_flVelocityModifier");
                        Utilities.SetStateChanged(p, "CBaseModelEntity", "m_clrRender");
                    }
                    _frozenPlayers.Remove(capturedVictim);
                }
            );

            return HookResult.Continue;
        }
    }
}
