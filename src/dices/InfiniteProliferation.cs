using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class InfiniteProliferation : DiceBlueprint
    {
        public override string ClassName => "InfiniteProliferation";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];

        // Track remaining respawns per player
        private readonly Dictionary<CCSPlayerController, int> _respawnsLeft = [];

        public InfiniteProliferation(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Necromancer");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "不死军团", "不死军团联动生效！");
            _respawnsLeft[player] = _config.Dices.InfiniteProliferation.MaxRespawns;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert($"🔄 无限增殖！可复活{_config.Dices.InfiniteProliferation.MaxRespawns}次，每次HP/甲减半！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (reason == DiceRemoveReason.Death)
            {
                // Don't remove on death — check respawn logic first
                return;
            }
            _ = _players.Remove(player);
            _ = _respawnsLeft.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _respawnsLeft.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !_players.Contains(victim)) return HookResult.Continue;
            if (!_respawnsLeft.TryGetValue(victim, out int left) || left <= 0) return HookResult.Continue;

            // Save weapons from attacker
            List<string> tmpWeaponList = [];
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker?.PlayerPawn?.Value?.WeaponServices != null)
            {
                foreach (CHandle<CBasePlayerWeapon> weapon in attacker.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    if (weapon?.Value?.DesignerName == null) continue;
                    string name = weapon.Value.DesignerName!;
                    if (name.Contains("knife") || name.Contains("bayonet") || name.Contains("c4"))
                        continue;
                    tmpWeaponList.Add(name);
                }
            }

            int remaining = left;
            int maxRespawns = _config.Dices.InfiniteProliferation.MaxRespawns;
            int deathsUsed = maxRespawns - remaining + 1; // this death + previous ones
            int newArmor = _config.Dices.InfiniteProliferation.BaseArmor; // Always full armor

            string victimName = victim.PlayerName;

            // Respawn
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
                            || victim?.PlayerPawn.Value.ItemServices == null
                            || victim.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                            return;

                        CCSPlayerPawn pawn = victim.PlayerPawn.Value;
                        victim.RemoveWeapons();
                        victim.GiveNamedItem("weapon_knife");

                        int originalMaxHP = pawn.MaxHealth;

                        // Calculate halved HP
                        int newHP = originalMaxHP;
                        for (int i = 0; i < deathsUsed; i++)
                        {
                            newHP /= 2;
                            if (newHP < 1) newHP = 1;
                        }

                        pawn.Health = newHP;
                        pawn.MaxHealth = newHP;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

                        pawn.ArmorValue = newArmor;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                        victim.GiveNamedItem("item_assaultsuit"); // Full armor = helmet + vest

                        if (victim.Team == CsTeam.CounterTerrorist)
                        {
                            new CCSPlayer_ItemServices(pawn.ItemServices!.Handle) { HasDefuser = true };
                        }

                        // Restore weapons
                        if (tmpWeaponList.Count > 0)
                        {
                            foreach (string w in tmpWeaponList)
                                victim.GiveNamedItem(w);
                        }
                        else
                        {
                            victim.GiveNamedItem("weapon_ak47");
                            victim.GiveNamedItem("weapon_deagle");
                        }

                        // Decrement respawns
                        int newRemaining = remaining - 1;
                        _respawnsLeft[victim] = newRemaining;

                        victim.PrintToCenterAlert($"🔄 无限增殖！还剩{newRemaining}次复活 | HP:{newHP} 甲:{newArmor}");
                        Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_InfiniteProliferation_revived"].Value.Replace("{playerName}", victimName).Replace("{hp}", newHP.ToString()).Replace("{armor}", newArmor.ToString()).Replace("{left}", newRemaining.ToString())}");

                        if (newRemaining <= 0)
                        {
                            _ = _players.Remove(victim);
                            _ = _respawnsLeft.Remove(victim);
                        }
                    });
                });
            });

            return HookResult.Continue;
        }
    }
}
