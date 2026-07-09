using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class DivineResurrection : DiceBlueprint
    {
        public override string ClassName => "DivineResurrection";
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, float> _cooldowns = [];

        public DivineResurrection(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _cooldowns.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _cooldowns.Clear();
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || attacker.PlayerPawn?.Value == null || !attacker.PlayerPawn.Value.IsValid)
                return HookResult.Continue;

            if (_cooldowns.TryGetValue(attacker, out float cd) && (float)Server.CurrentTime < cd)
                return HookResult.Continue;

            if (_random.NextDouble() >= _config.Dices.DivineResurrection.Chance)
                return HookResult.Continue;

            // Find a dead teammate
            CCSPlayerController? deadTeammate = Utilities.GetPlayers()
                .Where(p => p != attacker && p.IsValid && p.TeamNum == attacker.TeamNum
                    && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                    && p.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                .OrderBy(_ => _random.Next())
                .FirstOrDefault();

            if (deadTeammate == null) return HookResult.Continue;

            CCSPlayerController capturedAttacker = attacker;
            CCSPlayerController capturedDead = deadTeammate;

            // Save attacker's weapons to give to revived teammate
            List<string> weaponList = [];
            if (attacker.PlayerPawn?.Value?.WeaponServices != null)
            {
                foreach (CHandle<CBasePlayerWeapon> weapon in attacker.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    if (weapon?.Value != null
                        && weapon.Value.IsValid
                        && weapon.Value.DesignerName != null)
                    {
                        string name = weapon.Value.DesignerName;
                        // Skip knife and C4
                        if (name.Contains("knife") || name.Contains("c4"))
                            continue;
                        weaponList.Add(name);
                    }
                }
            }

            // Only set cooldown when revive actually starts
            _cooldowns[attacker] = (float)Server.CurrentTime + _config.Dices.DivineResurrection.Cooldown;

            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    if (capturedDead?.PlayerPawn?.Value == null
                        || capturedDead.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                        return;

                    capturedDead.Respawn();

                    Server.NextFrame(() =>
                    {
                        if (capturedDead?.PlayerPawn?.Value == null
                            || capturedDead.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                            return;

                        CCSPlayerPawn pawn = capturedDead.PlayerPawn.Value;

                        // Give full armor
                        pawn.ArmorValue = 100;

                        // Strip default weapons and give new ones
                        capturedDead.RemoveWeapons();
                        // Give knife
                        capturedDead.GiveNamedItem("weapon_knife");
                        // Give defuser if CT
                        if (capturedDead.Team == CsTeam.CounterTerrorist)
                        {
                            CCSPlayer_ItemServices itemServices = new(pawn.ItemServices!.Handle)
                            {
                                HasDefuser = true
                            };
                        }
                        // Give weapons
                        if (weaponList.Count > 0)
                        {
                            foreach (string weaponName in weaponList)
                            {
                                capturedDead.GiveNamedItem(weaponName);
                            }
                        }
                        else
                        {
                            // Default loadout
                            capturedDead.GiveNamedItem("weapon_ak47");
                            capturedDead.GiveNamedItem("weapon_deagle");
                        }

                        // Notify both players
                        string prefix = _localizer["command.prefix"].Value;
                        capturedDead.PrintToChat(prefix + _localizer["dice_DivineResurrection_revived"].Value);
                        capturedAttacker.PrintToChat(prefix + _localizer["dice_DivineResurrection_reviver"].Value
                            .Replace("{player}", capturedDead.PlayerName));
                    });
                });
            });

            return HookResult.Continue;
        }
    }
}
