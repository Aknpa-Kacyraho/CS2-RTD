using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Respawn : DiceBlueprint
    {
        public override string ClassName => "Respawn";
        private bool _comboActive;
        public readonly Random _random = new();
        public override List<string> Events => [
            "EventPlayerDeath"
        ];

        public Respawn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Dragonborn");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "浴火重生", "浴火重生联动生效！");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            // ignore removal on death, because player will be removed after respawn
            if (reason == DiceRemoveReason.Death)
            {
                return;
            }
            _ = _players.Remove(player);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null
                || !_players.Contains(victim)
                || victim.PlayerPawn?.Value?.WeaponServices == null)
            {
                return HookResult.Continue;
            }
            // give weapons from attacker because player is dead and has no weapons in weaponsService (they are removed)
            List<string> tmpWeaponList = [];
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker?.PlayerPawn?.Value?.WeaponServices != null)
            {
                foreach (CHandle<CBasePlayerWeapon> weapon in attacker.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    // ignore unknown weapons
                    if (weapon == null
                        || !weapon.IsValid
                        || weapon.Value == null
                        || (weapon.Value != null && weapon.Value.DesignerName == null))
                    {
                        continue;
                    }
                    // ignore knife and C4
                    if (weapon.Value!.DesignerName == $"weapon_{CsItem.C4.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == "weapon_knife" // necessary because CsItem.Knife is not always this value
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.Knife.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}")
                    {
                        continue;
                    }
                    // add Designername
                    tmpWeaponList.Add(weapon.Value.DesignerName!);
                }
            }
            // respawn player
            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    // sanity checks
                    if (victim?.PlayerPawn?.Value == null
                        || !_players.Contains(victim)
                        || victim.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        return;
                    }
                    // respawn player
                    victim.Respawn();
                    // give weapons next frame to ensure player is respawned
                    Server.NextFrame(() =>
                    {
                        // sanity checks
                        if (victim?.PlayerPawn?.Value == null
                            || victim?.PlayerPawn?.Value.ItemServices == null
                            || victim.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        {
                            return;
                        }
                        // strip all other weapons
                        victim.RemoveWeapons();
                        // add default knife to player
                        _ = victim.GiveNamedItem($"weapon_knife");
                        // give defuser if player is CT
                        if (victim.Team == CsTeam.CounterTerrorist)
                        {
                            CCSPlayer_ItemServices itemServices = new(victim.PlayerPawn.Value.ItemServices.Handle)
                            {
                                HasDefuser = true
                            };
                        }
                        // give player weapons of attacker (if any)
                        if (tmpWeaponList.Count > 0)
                        {
                            foreach (string weapons in tmpWeaponList)
                            {
                                _ = victim.GiveNamedItem(weapons);
                            }
                        }
                        else
                        {
                            // give some default loadout if no weapons are available
                            _ = victim.GiveNamedItem(_config.Dices.Respawn.DefaultPrimaryWeapon);
                            _ = victim.GiveNamedItem(_config.Dices.Respawn.DefaultSecondaryWeapon);
                        }
                        // set armor for player
                        victim.PlayerPawn.Value.ArmorValue = 100;
                        // Dragonborn combo: rebirth with dragon vitality
                        if (_comboActive)
                        {
                            victim.PlayerPawn.Value.MaxHealth = 200;
                            victim.PlayerPawn.Value.Health = 200;
                            victim.PlayerPawn.Value.ArmorValue = 200;
                            Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
                            Utilities.SetStateChanged(victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                            victim.PrintToCenterAlert("🔥 浴火重生！龙裔之力加持！200HP 200护甲！");
                        }
                        // remove player from list to avoid respawning again
                        _ = _players.Remove(victim);
                    });
                });
            });
            return HookResult.Continue;
        }
    }
}
