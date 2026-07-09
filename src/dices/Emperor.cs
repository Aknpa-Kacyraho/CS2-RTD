using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Emperor : DiceBlueprint
    {
        public override string ClassName => "Emperor";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
        private readonly Dictionary<CCSPlayerController, bool> _usedResurrection = [];

        public Emperor(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _comboActive = DiceSynergy.HasPartner(player, "Empress");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "王权永恒", "王权永恒联动生效！");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _usedResurrection.Clear();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid)
            {
                return HookResult.Continue;
            }

            // Don't respawn the emperor himself
            if (_players.Contains(victim))
            {
                return HookResult.Continue;
            }

            // Check if any emperor is on the same team and haven't used resurrection on this player
            CCSPlayerController? emperor = null;
            foreach (var emp in _players)
            {
                if (emp == null || !emp.IsValid || emp.TeamNum != victim.TeamNum)
                    continue;
                if (!_usedResurrection.TryGetValue(victim, out bool used) || !used)
                {
                    emperor = emp;
                    break;
                }
            }

            if (emperor == null) return HookResult.Continue;

            // Save victim's weapons
            List<string> tmpWeaponList = [];
            if (victim.PlayerPawn?.Value?.WeaponServices != null)
            {
                foreach (CHandle<CBasePlayerWeapon> weapon in victim.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    if (weapon == null || !weapon.IsValid || weapon.Value == null
                        || weapon.Value.DesignerName == null)
                        continue;
                    if (weapon.Value.DesignerName == $"weapon_{CsItem.C4.ToString().ToLower()}"
                        || weapon.Value.DesignerName == "weapon_knife"
                        || weapon.Value.DesignerName == $"weapon_{CsItem.Knife.ToString().ToLower()}"
                        || weapon.Value.DesignerName == $"weapon_{CsItem.KnifeCT.ToString().ToLower()}"
                        || weapon.Value.DesignerName == $"weapon_{CsItem.KnifeT.ToString().ToLower()}"
                        || weapon.Value.DesignerName == $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower()}"
                        || weapon.Value.DesignerName == $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower()}")
                        continue;
                    tmpWeaponList.Add(weapon.Value.DesignerName!);
                }
            }

            _usedResurrection[victim] = true;

            // Respawn the teammate (same pattern as Respawn.cs)
            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    if (victim?.PlayerPawn?.Value == null
                        || victim.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                        return;

                    victim.Respawn();

                    Server.NextFrame(() =>
                    {
                        if (victim?.PlayerPawn?.Value == null
                            || victim?.PlayerPawn?.Value.ItemServices == null
                            || victim.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                            return;

                        victim.RemoveWeapons();
                        _ = victim.GiveNamedItem("weapon_knife");

                        if (victim.Team == CsTeam.CounterTerrorist)
                        {
                            CCSPlayer_ItemServices itemServices = new(victim.PlayerPawn.Value.ItemServices.Handle)
                            {
                                HasDefuser = true
                            };
                        }

                        if (tmpWeaponList.Count > 0)
                        {
                            foreach (string weapons in tmpWeaponList)
                            {
                                _ = victim.GiveNamedItem(weapons);
                            }
                        }
                        else
                        {
                            _ = victim.GiveNamedItem(_config.Dices.Respawn.DefaultPrimaryWeapon);
                            _ = victim.GiveNamedItem(_config.Dices.Respawn.DefaultSecondaryWeapon);
                        }

                        victim.PlayerPawn.Value.ArmorValue = 100;
                        // Empress combo: revived teammates get royal blessing
                        if (_comboActive)
                        {
                            victim.PlayerPawn.Value.MaxHealth = 150;
                            victim.PlayerPawn.Value.Health = 150;
                            victim.PlayerPawn.Value.ArmorValue = 150;
                            Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
                            Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                            victim.PrintToCenterAlert("👑 王权永恒！150HP 150护甲！");
                        }
                    });
                });
            });

            return HookResult.Continue;
        }
    }
}
