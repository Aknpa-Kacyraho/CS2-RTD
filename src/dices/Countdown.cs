using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Countdown : DiceBlueprint
    {
        public override string ClassName => "Countdown";
        public override List<string> Listeners => ["OnTick"];

        private readonly Dictionary<CCSPlayerController, CountdownState> _states = [];

        private class CountdownState
        {
            public float EndTime;
            public Vector SpawnPosition = null!;
            public QAngle SpawnAngles = null!;
            public List<string> OriginalWeapons = [];
            public int OriginalHP;
            public int OriginalArmor;
            public bool OriginalHelmet;
            public bool Triggered;
            public bool ComboActive;
        }

        public Countdown(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            // Save spawn point (current position at round start) and original weapons
            var weapons = new List<string>();
            if (pawn.WeaponServices?.MyWeapons != null)
            {
                foreach (var wh in pawn.WeaponServices.MyWeapons)
                {
                    if (wh?.Value?.DesignerName == null) continue;
                    string name = wh.Value.DesignerName;
                    if (name.Contains("knife") || name.Contains("bayonet")) continue;
                    weapons.Add(name);
                }
            }

            // Save original armor
            int origArmor = pawn.ArmorValue;
            bool origHelmet = pawn.ItemServices != null
                && new CCSPlayer_ItemServices(pawn.ItemServices.Handle).HasHelmet;

            var state = new CountdownState
            {
                EndTime = (float)Server.CurrentTime + _config.Dices.Countdown.Countdown,
                SpawnPosition = new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin!.Y, pawn.AbsOrigin!.Z),
                SpawnAngles = new QAngle(pawn.AbsRotation!.X, pawn.AbsRotation!.Y, pawn.AbsRotation!.Z),
                OriginalWeapons = weapons,
                OriginalHP = pawn.Health,
                OriginalArmor = origArmor,
                OriginalHelmet = origHelmet,
                Triggered = false
            };

            _players.Add(player);
            bool hasCombo = DiceSynergy.HasPartner(player, "Rewind");
            state.ComboActive = hasCombo;
            if (hasCombo)
                DiceSynergy.AnnounceCombo(player, "时空主宰", "时空主宰联动生效！");
            _states[player] = state;

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _states.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _states.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_states.Count == 0) return;

            float now = (float)Server.CurrentTime;

            foreach (var kv in _states.ToList())
            {
                var player = kv.Key;
                var state = kv.Value;

                if (state.Triggered) continue;

                float remaining = state.EndTime - now;

                if (remaining > 0)
                {
                    if (Server.TickCount % 64 == 0)
                    {
                        int sec = (int)Math.Ceiling(remaining);
                        player?.PrintToCenterAlert($"倒计时: {sec}秒");
                    }
                    continue;
                }

                // Time's up! Teleport back to spawn, full HP, original armor, original weapons
                state.Triggered = true;

                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                {
                    _ = _states.Remove(player!);
                    continue;
                }

                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    _ = _states.Remove(player);
                    continue;
                }

                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                int fullHP = state.ComboActive ? Math.Max(state.OriginalHP, 100) + 50 : Math.Max(state.OriginalHP, 100);

                pawn.Teleport(state.SpawnPosition, state.SpawnAngles, new Vector(0, 0, 0));
                pawn.Health = fullHP;
                pawn.MaxHealth = Math.Max(pawn.MaxHealth, fullHP);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

                // Restore original weapons (RemoveWeapons may strip armor in CS2, so do this BEFORE setting armor)
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
                if (state.OriginalWeapons.Count > 0)
                {
                    foreach (string w in state.OriginalWeapons)
                        player.GiveNamedItem(w);
                }

                // Restore armor AFTER weapons (RemoveWeapons may have stripped armor)
                pawn.ArmorValue = state.OriginalArmor > 0 ? state.OriginalArmor : 100;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                if (state.OriginalHelmet && pawn.ItemServices != null)
                    new CCSPlayer_ItemServices(pawn.ItemServices.Handle) { HasHelmet = true };

                // CT gets defuser
                if (player.Team == CsTeam.CounterTerrorist && pawn.ItemServices != null)
                {
                    new CCSPlayer_ItemServices(pawn.ItemServices.Handle) { HasDefuser = true };
                }

                player.PrintToCenterAlert("倒计时结束！已回溯至出生点！满血满甲！");

                // Remove from this dice (one-time effect)
                _ = _states.Remove(player);
                _ = _players.Remove(player);
            }
        }
    }
}
