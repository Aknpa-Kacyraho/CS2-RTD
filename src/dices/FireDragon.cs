using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class FireDragon : DiceBlueprint
    {
        public override string ClassName => "FireDragon";
        public override bool CanBeDrawn => false;
        public override List<string> Events => ["EventPlayerHurt"];
        public override List<string> Listeners => ["OnTick"];
        public override List<string> Precache => [];

        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];
        private readonly Dictionary<CCSPlayerController, int> _originalArmor = [];
        private readonly Dictionary<ulong, float> _burnEndTime = [];
        private readonly Dictionary<ulong, int> _burnDps = [];

        public FireDragon(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

            pawn.MaxHealth = _config.Dices.FireDragon.BonusHP;
            pawn.Health = _config.Dices.FireDragon.BonusHP;
            pawn.ArmorValue = _config.Dices.FireDragon.BonusArmor;
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
            _burnEndTime.Clear();
            _burnDps.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid
                || !_players.Contains(attacker) || victim.Pawn?.Value == null || !victim.Pawn.Value.IsValid)
                return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            float burnDur = _config.Dices.FireDragon.BurnDuration;
            int dps = _config.Dices.FireDragon.BurnDamagePerSec;

            float existingEnd = _burnEndTime.GetValueOrDefault(victim.SteamID, 0f);
            float newEnd = Math.Max(now, existingEnd) + burnDur;
            _burnEndTime[victim.SteamID] = newEnd;

            int existingDps = _burnDps.GetValueOrDefault(victim.SteamID, 0);
            _burnDps[victim.SteamID] = existingDps + dps;

            victim.PrintToCenterAlert($"🔥 被火巨龙灼烧！-{_burnDps[victim.SteamID]}HP/s！");
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_burnEndTime.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var (steamId, endTime) in _burnEndTime.ToList())
            {
                if (now >= endTime)
                {
                    _burnEndTime.Remove(steamId);
                    _burnDps.Remove(steamId);
                    continue;
                }

                var player = Utilities.GetPlayers().FirstOrDefault(x => x.SteamID == steamId);
                if (player?.PlayerPawn?.Value is not CCSPlayerPawn pawn || !pawn.IsValid
                    || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                if (_burnEndTime.TryGetValue(steamId, out float bEnd) && now < bEnd)
                {
                    int dps = _burnDps.GetValueOrDefault(steamId, 20);
                    pawn.Health -= dps;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    if (pawn.Health <= 0)
                    {
                        if (!player.IsBot && !player.IsHLTV)
                            pawn.CommitSuicide(false, true);
                        else
                        {
                            try { pawn.CommitSuicide(false, true); }
                            catch
                            {
                                pawn.Health = 0;
                                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                            }
                        }
                    }
                }
            }
        }
    }
}
