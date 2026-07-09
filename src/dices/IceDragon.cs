using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class IceDragon : DiceBlueprint
    {
        public override string ClassName => "IceDragon";
        public override bool CanBeDrawn => false;
        public override List<string> Events => ["EventPlayerHurt"];
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];
        private readonly Dictionary<CCSPlayerController, int> _originalArmor = [];
        private readonly Dictionary<ulong, float> _frozenUntil = [];
        private readonly HashSet<ulong> _frozenSteamIDs = [];

        public IceDragon(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

            pawn.MaxHealth = _config.Dices.IceDragon.BonusHP;
            pawn.Health = _config.Dices.IceDragon.BonusHP;
            pawn.ArmorValue = _config.Dices.IceDragon.BonusArmor;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
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
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Remove(p);
            _players.Clear();
            _originalMaxHealth.Clear();
            _originalArmor.Clear();
            _frozenUntil.Clear();
            _frozenSteamIDs.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
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

            float now = (float)Server.CurrentTime;
            float freezeTime = _config.Dices.IceDragon.FreezeDuration;
            float existingEnd = _frozenUntil.GetValueOrDefault(victim.SteamID, 0f);
            float newEnd = Math.Max(now, existingEnd) + freezeTime;
            _frozenUntil[victim.SteamID] = newEnd;
            _frozenSteamIDs.Add(victim.SteamID);

            CCSPlayerPawn victimPawn = victim.PlayerPawn.Value;
            victimPawn.Render = Color.FromArgb(255, 100, 200, 255);
            Utilities.SetStateChanged(victimPawn, "CBaseModelEntity", "m_clrRender");
            MoveLockManager.Lock(victim, "IceDragon");

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
                    _frozenSteamIDs.Remove(steamId);
                    var p = Utilities.GetPlayers().FirstOrDefault(x => x.SteamID == steamId);
                    if (p?.PlayerPawn?.Value is CCSPlayerPawn paw && paw.IsValid)
                    {
                        MoveLockManager.Unlock(p, "IceDragon");
                        paw.Render = Color.FromArgb(255, 255, 255, 255);
                        Utilities.SetStateChanged(paw, "CBaseModelEntity", "m_clrRender");
                    }
                    continue;
                }

                var player = Utilities.GetPlayers().FirstOrDefault(x => x.SteamID == steamId);
                if (player?.PlayerPawn?.Value is CCSPlayerPawn pawn && pawn.IsValid)
                {
                    pawn.Render = Color.FromArgb(255, 100, 200, 255);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                    MoveLockManager.Lock(player, "IceDragon");
                }
            }
        }
    }
}
