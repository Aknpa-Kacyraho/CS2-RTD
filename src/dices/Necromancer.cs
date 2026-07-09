using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System;
using System.Drawing;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Necromancer : DiceBlueprint
    {
        public override string ClassName => "Necromancer";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private readonly Dictionary<ulong, float> _invincibilityEndTime = [];
        private readonly Dictionary<ulong, Vector> _deathPositions = [];

        public Necromancer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "InfiniteProliferation");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "不死军团", "不死军团联动生效！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _invincibilityEndTime.Clear();
            _deathPositions.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid) return HookResult.Continue;

            if (_invincibilityEndTime.TryGetValue(victim.SteamID, out float endTime)
                && (float)Server.CurrentTime < endTime)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? deadPlayer = @event.Userid;
            if (deadPlayer == null || !deadPlayer.IsValid) return HookResult.Continue;

            // Save death position before respawn
            if (deadPlayer.PlayerPawn?.Value?.AbsOrigin != null)
            {
                Vector deathPos = new(
                    deadPlayer.PlayerPawn.Value.AbsOrigin.X,
                    deadPlayer.PlayerPawn.Value.AbsOrigin.Y,
                    deadPlayer.PlayerPawn.Value.AbsOrigin.Z);
                _deathPositions[deadPlayer.SteamID] = deathPos;
            }

            // Find a Necromancer on the same team who has enough HP
            int reviveCost = (int)float.Round(_config.Dices.Necromancer.ReviveHPCost);
            CCSPlayerController? necro = null;

            foreach (var candidate in _players)
            {
                if (candidate == null || !candidate.IsValid || candidate == deadPlayer) continue;
                if (candidate.TeamNum != deadPlayer.TeamNum) continue;
                if (candidate.PlayerPawn?.Value == null || !candidate.PlayerPawn.Value.IsValid) continue;
                if (candidate.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                if (candidate.PlayerPawn.Value.Health <= reviveCost) continue;

                necro = candidate;
                break;
            }

            if (necro == null) return HookResult.Continue;

            // Deduct HP from necromancer
            CCSPlayerPawn necroPawn = necro.PlayerPawn!.Value!;
            necroPawn.Health -= reviveCost;
            Utilities.SetStateChanged(necroPawn, "CBaseEntity", "m_iHealth");

            // Save death position for respawn
            ulong deadSteamID = deadPlayer.SteamID;
            Vector savedDeathPos = new(0, 0, 0);
            bool hasDeathPos = _deathPositions.TryGetValue(deadSteamID, out Vector dp);
            if (hasDeathPos) savedDeathPos = dp;

            CCSPlayerController capturedDead = deadPlayer;
            CCSPlayerController capturedNecro = necro;

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

                        capturedDead.RemoveWeapons();
                        capturedDead.GiveNamedItem("weapon_knife");

                        // Teleport to death location (deferred to avoid engine override)
                        if (hasDeathPos)
                        {
                            Vector posToUse = savedDeathPos;
                            CCSPlayerPawn pawnToTP = capturedDead.PlayerPawn.Value;
                            new CounterStrikeSharp.API.Modules.Timers.Timer(0.1f, () =>
                            {
                                if (pawnToTP == null || !pawnToTP.IsValid) return;
                                pawnToTP.Teleport(posToUse,
                                    pawnToTP.EyeAngles ?? new QAngle(0, 0, 0),
                                    new Vector(0, 0, 0));
                            });
                        }

                        // Give 1 second invincibility
                        ulong revivedID = capturedDead.SteamID;
                        _invincibilityEndTime[revivedID] = (float)Server.CurrentTime + 1f;
                        CCSPlayerPawn revivedPawn = capturedDead.PlayerPawn.Value;
                        revivedPawn.Render = Color.FromArgb(128, 255, 255, 255);
                        Utilities.SetStateChanged(revivedPawn, "CBaseModelEntity", "m_clrRender");

                        // Remove weapons to prevent shooting during invincibility
                        var weaponList = new List<string>();
                        if (revivedPawn.WeaponServices != null)
                        {
                            foreach (var weapon in revivedPawn.WeaponServices.MyWeapons)
                            {
                                if (weapon?.Value != null && weapon.Value.IsValid
                                    && weapon.Value.DesignerName != null
                                    && !weapon.Value.DesignerName.Contains("knife"))
                                {
                                    weaponList.Add(weapon.Value.DesignerName);
                                }
                            }
                        }
                        capturedDead.RemoveWeapons();
                        capturedDead.GiveNamedItem("weapon_knife");

                        // Restore after 1 second
                        CCSPlayerController capDead2 = capturedDead;
                        new CounterStrikeSharp.API.Modules.Timers.Timer(1f, () =>
                        {
                            if (capDead2?.PlayerPawn?.Value != null && capDead2.PlayerPawn.Value.IsValid)
                            {
                                capDead2.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                                Utilities.SetStateChanged(capDead2.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");

                                // Give weapons back
                                foreach (var wn in weaponList)
                                    capDead2.GiveNamedItem(wn);
                            }
                            _invincibilityEndTime.Remove(revivedID);
                        });

                        capturedDead.PrintToCenterAlert("💀 死灵法师在死亡地点复活了你！1秒无敌！");
                        capturedNecro?.PrintToCenterAlert($"💀 你复活了 {capturedDead.PlayerName}！-{reviveCost}HP");
                        Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Necromancer_revive"].Value.Replace("{necroName}", capturedNecro.PlayerName).Replace("{deadName}", capturedDead.PlayerName)}");
                    });
                });
            });

            return HookResult.Continue;
        }
    }
}
