using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class SwordSaint : DiceBlueprint
    {
        public override string ClassName => "SwordSaint";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnTick",
            "OnPlayerTakeDamagePre"
        ];
        private readonly Dictionary<CCSPlayerController, CBasePlayerWeapon?> _previousWeapon = [];

        public SwordSaint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Cutter");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "剑刃风暴", "剑刃风暴联动生效！");
            _previousWeapon[player] = null;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            // Restore normal render
            if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
            _ = _players.Remove(player);
            _ = _previousWeapon.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
            _previousWeapon.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            foreach (CCSPlayerController player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    CBasePlayerWeapon? currentWeapon = pawn.WeaponServices?.ActiveWeapon?.Value;

                    if (!_previousWeapon.TryGetValue(player, out CBasePlayerWeapon? prev) || prev != currentWeapon)
                    {
                        _previousWeapon[player] = currentWeapon;

                        if (currentWeapon != null && currentWeapon.DesignerName != null
                            && currentWeapon.DesignerName.Contains("knife"))
                        {
                            // Light blue render when holding knife
                            pawn.Render = Color.FromArgb(255, 100, 200, 255);
                            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                        }
                        else
                        {
                            // Restore normal render
                            pawn.Render = Color.FromArgb(255, 255, 255, 255);
                            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            CCSPlayerPawn? pawn = victim.PlayerPawn.Value;
            if (pawn?.WeaponServices?.ActiveWeapon?.Value == null)
                return HookResult.Continue;

            string weaponName = pawn.WeaponServices.ActiveWeapon.Value.DesignerName;
            if (weaponName == null || !weaponName.Contains("knife"))
                return HookResult.Continue;

            // Check if damage type is bullet
            if ((info.BitsDamageType & DamageTypes_t.DMG_BULLET) != 0)
            {
                info.Damage = 0;
                if (_comboActive && info.Attacker?.Value != null)
                {
                    // Stun attacker for 0.5s when they shoot a knife-holding SwordSaint
                    var attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
                    if (attacker != null && attacker.IsValid && attacker.PlayerPawn?.Value != null)
                    {
                        attacker.PlayerPawn.Value.VelocityModifier = 0.01f;
                        Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                        var capturedAttacker = attacker;
                        new CounterStrikeSharp.API.Modules.Timers.Timer(0.5f, () =>
                        {
                            if (capturedAttacker?.PlayerPawn?.Value != null && capturedAttacker.PlayerPawn.Value.IsValid)
                            {
                                capturedAttacker.PlayerPawn.Value.VelocityModifier = 1.0f;
                                Utilities.SetStateChanged(capturedAttacker.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                            }
                        });
                    }
                }
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
    }
}
