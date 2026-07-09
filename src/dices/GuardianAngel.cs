using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class GuardianAngel : DiceBlueprint
    {
        public override string ClassName => "GuardianAngel";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        // Track by SteamID for robustness (entity indices can change)
        private readonly HashSet<ulong> _hasAngel = [];
        // Track entities currently being saved (to prevent double-trigger within same tick)
        private readonly HashSet<nint> _processingSave = [];

        public GuardianAngel(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Thorns") || DiceSynergy.HasPartner(player, "Nirvana");
            if (DiceSynergy.HasPartner(player, "Thorns"))
                DiceSynergy.AnnounceCombo(player, "圣光荆棘", "圣光荆棘联动生效！");
            if (DiceSynergy.HasPartner(player, "Nirvana"))
                DiceSynergy.AnnounceCombo(player, "菲尼克斯", "菲尼克斯联动生效！");
            _hasAngel.Add(player.SteamID);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _hasAngel.Remove(player.SteamID);
        }

        public override void Reset()
        {
            _players.Clear();
            _hasAngel.Clear();
            _processingSave.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid)
                return HookResult.Continue;

            CCSPlayerController? player = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (player == null || !player.IsValid || !_hasAngel.Contains(player.SteamID))
                return HookResult.Continue;

            // Prevent double-processing within the same tick (multiple damage events from one shot)
            if (_processingSave.Contains(entity.Handle))
                return HookResult.Continue;

            int healthAfterDamage = entity.Health - (int)float.Round(info.Damage);
            if (healthAfterDamage > 0)
                return HookResult.Continue;

            // Lethal damage blocked!
            _processingSave.Add(entity.Handle);
            info.Damage = 0;
            _hasAngel.Remove(player.SteamID);

            // Immediately prevent all further damage
            entity.TakesDamage = false;

            Server.NextFrame(() =>
            {
                _processingSave.Remove(entity.Handle);

                if (entity == null || !entity.IsValid)
                    return;

                int restoreHp = Math.Min(_comboActive ? _config.Dices.GuardianAngel.RestoreHealth * 2 : _config.Dices.GuardianAngel.RestoreHealth, entity.MaxHealth);
                entity.Health = restoreHp;
                entity.MaxHealth = Math.Max(restoreHp, entity.MaxHealth);
                Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(entity, "CBaseEntity", "m_iMaxHealth");

                // Re-enable damage after invincibility window
                _ = new CounterStrikeSharp.API.Modules.Timers.Timer(
                    _config.Dices.GuardianAngel.InvincibilitySeconds,
                    () =>
                    {
                        if (entity != null && entity.IsValid)
                        {
                            entity.TakesDamage = true;
                        }
                    }
                );

                if (player != null && player.IsValid)
                {
                    string savedMsg = _localizer["dice_GuardianAngel_saved"].Value
                        .Replace("{hp}", restoreHp.ToString());
                    player.PrintToCenter(savedMsg);
                    player.PrintToChat(_localizer["command.prefix"].Value + savedMsg);
                }
            });

            return HookResult.Changed;
        }
    }
}
