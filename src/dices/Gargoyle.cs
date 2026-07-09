using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Gargoyle : DiceBlueprint
    {
        public override string ClassName => "Gargoyle";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerButtonsChanged", "OnTick", "OnPlayerTakeDamagePre"];

        public override float GetCooldownRemaining(CCSPlayerController player)
            => _cooldowns.TryGetValue(player, out float cd) ? Math.Max(0, cd - (float)Server.CurrentTime) : 0f;
        private readonly Dictionary<CCSPlayerController, float> _cooldowns = [];
        private readonly Dictionary<CCSPlayerController, float> _petrifyEndTime = [];
        private readonly Dictionary<CCSPlayerController, List<string>> _savedWeapons = [];
        private readonly Dictionary<CCSPlayerController, int> _savedArmor = [];
        private readonly Dictionary<CCSPlayerController, bool> _savedHelmet = [];

        public Gargoyle(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Titanfall");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "泰坦神像", "石像冷却减半，泰坦提前15s觉醒+50%移速+50%伤害！");
            _cooldowns[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            Unpetrify(player);
            _ = _players.Remove(player);
            _ = _cooldowns.Remove(player);
            _ = _petrifyEndTime.Remove(player);
            _ = _savedWeapons.Remove(player);
            _ = _savedArmor.Remove(player);
            _ = _savedHelmet.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Unpetrify(p);
            _players.Clear(); _cooldowns.Clear(); _petrifyEndTime.Clear(); _savedWeapons.Clear();
            _savedArmor.Clear(); _savedHelmet.Clear();
        }

        public override void Destroy() => Reset();

        private void Unpetrify(CCSPlayerController player)
        {
            MoveLockManager.Unlock(player, "Gargoyle");

            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            // Restore armor first
            if (_savedArmor.TryGetValue(player, out int savedArmor))
            {
                pawn.ArmorValue = savedArmor;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                _savedArmor.Remove(player);
            }
            if (_savedHelmet.TryGetValue(player, out bool hasHelmet) && hasHelmet
                && pawn.ItemServices != null)
            {
                new CCSPlayer_ItemServices(pawn.ItemServices.Handle) { HasHelmet = true };
                _savedHelmet.Remove(player);
            }

            // Restore saved weapons (only if player is still alive)
            if (_savedWeapons.TryGetValue(player, out var weaponNames)
                && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
            {
                foreach (string weaponName in weaponNames)
                {
                    player.GiveNamedItem(weaponName);
                }
                _savedWeapons.Remove(player);
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float now = (float)Server.CurrentTime;
            if (_cooldowns.TryGetValue(player, out float cd) && now < cd) return;

            float duration = _config.Dices.Gargoyle.Duration;
            _cooldowns[player] = now + (_comboActive ? _config.Dices.Gargoyle.Cooldown / 2f : _config.Dices.Gargoyle.Cooldown);
            _petrifyEndTime[player] = now + duration;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            // Save armor before RemoveWeapons strips it
            _savedArmor[player] = pawn.ArmorValue;
            var itemServices = pawn.ItemServices;
            _savedHelmet[player] = itemServices != null && new CCSPlayer_ItemServices(itemServices.Handle).HasHelmet;

            // Save weapons before removing
            var weaponServices = pawn.WeaponServices;
            if (weaponServices?.MyWeapons != null)
            {
                var savedList = new List<string>();
                foreach (var wh in weaponServices.MyWeapons.ToList())
                {
                    if (wh?.Value != null && wh.Value.IsValid && !string.IsNullOrEmpty(wh.Value.DesignerName))
                    {
                        savedList.Add(wh.Value.DesignerName);
                    }
                }
                _savedWeapons[player] = savedList;
            }

            // Remove weapons during petrification (this also strips armor in CS2)
            player.RemoveWeapons();

            // Lock movement
            MoveLockManager.Lock(player, "Gargoyle");

            // Stone visual — gray tint
            pawn.Render = Color.FromArgb(255, 140, 140, 150);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            player.PrintToCenterAlert("🗿 石像形态！3秒免疫子弹，攻击者-10HP！");
        }

        // Block bullet damage while petrified, reflect 10 damage to attacker
        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;
            if (_petrifyEndTime.TryGetValue(victim, out float end) && (float)Server.CurrentTime < end)
            {
                // Reflect 10 damage to attacker via health reduction
                CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
                if (attacker != null && attacker.IsValid && attacker != victim
                    && attacker.PlayerPawn?.Value != null && attacker.PlayerPawn.Value.IsValid
                    && attacker.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    CCSPlayerPawn attackerPawn = attacker.PlayerPawn.Value;
                    attackerPawn.Health -= 10;
                    Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
                    if (attackerPawn.Health <= 0)
                    {
                        if (!attacker.IsBot && !attacker.IsHLTV)
                            attackerPawn.CommitSuicide(false, true);
                        else
                        {
                            try { attackerPawn.CommitSuicide(false, true); }
                            catch
                            {
                                attackerPawn.Health = 0;
                                Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
                            }
                        }
                    }
                }

                // Block bullet damage only — melee/explosives/fall still hurt
                if ((info.BitsDamageType & DamageTypes_t.DMG_BULLET) != 0)
                {
                    info.Damage = 0;
                    return HookResult.Changed;
                }
            }
            return HookResult.Continue;
        }

        // Maintain locked state and restore on expiry
        public void OnTick()
        {
            if (_petrifyEndTime.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var kv in _petrifyEndTime.ToList())
            {
                var player = kv.Key;
                if (now >= kv.Value)
                {
                    Unpetrify(player);
                    _ = _petrifyEndTime.Remove(player);
                    player?.PrintToCenterAlert("🗿 石化解除了!");
                    continue;
                }

                // Maintain movement lock (engine may reset)
                MoveLockManager.Lock(player, "Gargoyle");
            }
        }
    }
}
