using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Titanfall : DiceBlueprint
    {
        public override string ClassName => "Titanfall";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private readonly Dictionary<CCSPlayerController, float> _releaseTime = [];
        private readonly Dictionary<CCSPlayerController, bool> _released = [];
        private readonly Dictionary<CCSPlayerController, float> _lockdownStart = [];
        private readonly Dictionary<CCSPlayerController, int> _initialHP = [];
        private readonly Dictionary<CCSPlayerController, int> _initialArmor = [];

        public Titanfall(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Gargoyle");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "泰坦神像", "泰坦提前15s觉醒");

            float lockdown = _comboActive
                ? Math.Max(_config.Dices.Titanfall.LockdownSeconds - 15f, 5f)
                : _config.Dices.Titanfall.LockdownSeconds;

            float now = (float)Server.CurrentTime;
            _releaseTime[player] = now + lockdown;
            _lockdownStart[player] = now;
            _released[player] = false;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            _initialHP[player] = pawn.Health;
            _initialArmor[player] = Math.Max(pawn.ArmorValue, 100);

            // Ensure full armor from the start (helmet + vest)
            pawn.ArmorValue = Math.Max(pawn.ArmorValue, 100);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            player.GiveNamedItem("item_assaultsuit");

            MoveLockManager.Lock(player, "Titanfall");

            player.PrintToCenterAlert($"🚀 泰坦陨落！{lockdown}s 后觉醒...");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            MoveLockManager.Unlock(player, "Titanfall");
            DamageBonusManager.Unregister(player, "Titanfall");
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
            _ = _players.Remove(player);
            _ = _releaseTime.Remove(player);
            _ = _released.Remove(player);
            _ = _lockdownStart.Remove(player);
            _ = _initialHP.Remove(player);
            _ = _initialArmor.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                MoveLockManager.Unlock(p, "Titanfall");
                if (p?.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
            _players.Clear(); _releaseTime.Clear(); _released.Clear();
            _lockdownStart.Clear(); _initialHP.Clear(); _initialArmor.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    if (!_releaseTime.TryGetValue(player, out float releaseAt)) continue;
                    if (!_released.TryGetValue(player, out bool released)) continue;

                    if (!released)
                    {
                        MoveLockManager.Lock(player, "Titanfall");

                        float remaining = releaseAt - now;
                        float lockStart = _lockdownStart.GetValueOrDefault(player, now);
                        float totalLock = releaseAt - lockStart;
                        float progress = totalLock > 0 ? Math.Min((now - lockStart) / totalLock, 1f) : 1f;

                        int targetHP = _config.Dices.Titanfall.TitanHP;
                        int targetArmor = _config.Dices.Titanfall.TitanArmor;
                        int startHP = _initialHP.GetValueOrDefault(player, 100);
                        int startArmor = _initialArmor.GetValueOrDefault(player, 0);

                        pawn.MaxHealth = (int)float.Round(startHP + (targetHP - startHP) * progress);
                        pawn.Health = pawn.MaxHealth;
                        pawn.ArmorValue = (int)float.Round(startArmor + (targetArmor - startArmor) * progress);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

                        if (remaining <= 0)
                        {
                            _released[player] = true;
                            MoveLockManager.Unlock(player, "Titanfall");

                            pawn.MaxHealth = targetHP;
                            pawn.Health = targetHP;
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                            pawn.VelocityModifier = 1.5f;
                            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

                            DamageBonusManager.Register(player, "Titanfall", _config.Dices.Titanfall.DamageMultiplier - 1.0f);

                            player.PrintToCenterAlert("⚡ 泰坦觉醒！+50%伤害 +50%移速！");
                            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Titanfall_awaken"].Value.Replace("{playerName}", player.PlayerName)}");
                        }
                        else if (Server.TickCount % 64 == 0)
                        {
                            player.PrintToCenterAlert($"🚀 泰坦陨落中... HP:{pawn.Health}/{pawn.MaxHealth} 甲:{pawn.ArmorValue} {Math.Ceiling(remaining)}s");
                        }
                    }
                    else
                    {
                        if (pawn.VelocityModifier < 1.4f)
                        {
                            pawn.VelocityModifier = 1.5f;
                            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                        }
                    }
                }
                catch { }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (!_released.TryGetValue(attacker, out bool released) || !released) return HookResult.Continue;
            if (DamageBonusManager.IsHighest(attacker, "Titanfall"))
            {
                float effective = DamageBonusManager.GetEffective(attacker);
                info.Damage = (int)(info.Damage * (1 + effective));
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }
    }
}
