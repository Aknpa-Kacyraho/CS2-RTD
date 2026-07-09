using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class WheelOfFate : DiceBlueprint
    {
        public override string ClassName => "WheelOfFate";
        public readonly Random _random = new();
        public override List<string> Events => [
            "EventPlayerDeath"
        ];

        public WheelOfFate(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
            NotifyStatus(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            // Don't remove on death — player might revive
            if (reason == DiceRemoveReason.Death)
            {
                return;
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
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

            // Roll chance for revival
            double roll = _random.NextDouble();
            if (roll >= _config.Dices.WheelOfFate.ReviveChance)
            {
                // Let player know the wheel spun but didn't favor them this time
                string failedMsg = _localizer["dice_WheelOfFate_failed"].Value;
                if (!string.IsNullOrEmpty(failedMsg))
                    victim.PrintToCenterAlert(failedMsg.Replace("{playerName}", victim.PlayerName));
                return HookResult.Continue;
            }

            // Save weapons
            List<string> tmpWeaponList = [];
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker?.PlayerPawn?.Value?.WeaponServices != null)
            {
                foreach (CHandle<CBasePlayerWeapon> weapon in attacker.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    if (weapon == null || !weapon.IsValid || weapon.Value == null
                        || weapon.Value.DesignerName == null)
                        continue;
                    if (weapon.Value!.DesignerName == $"weapon_{CsItem.C4.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == "weapon_knife"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.Knife.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeCT.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeT.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower()}")
                        continue;
                    tmpWeaponList.Add(weapon.Value.DesignerName!);
                }
            }

            // Respawn player (same pattern as Respawn.cs)
            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    if (victim?.PlayerPawn?.Value == null
                        || !_players.Contains(victim)
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
                        Utilities.SetStateChanged(victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");

                        NotifyStatus(victim, ClassName, new()
                        {
                            { "playerName", victim.PlayerName }
                        });
                        Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_WheelOfFate_broadcast"].Value.Replace("{playerName}", victim.PlayerName)}");
                    });
                });
            });

            return HookResult.Continue;
        }
    }
}
