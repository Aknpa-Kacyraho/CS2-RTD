using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class ImposterSyndrome : DiceBlueprint
    {
        public override string ClassName => "ImposterSyndrome";
        public override List<string> Listeners => [
            "OnTick",
            "OnEntitySpawned"
        ];
        private readonly Dictionary<CCSPlayerController, float> _nextCheckTime = [];
        private readonly Dictionary<CCSPlayerController, float> _nextDecoyGrantTime = [];
        private readonly Dictionary<CCSPlayerController, List<(CDynamicProp?, CDynamicProp?)>> _decoyGlows = [];

        public ImposterSyndrome(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);

            _nextCheckTime[player] = 0;
            _nextDecoyGrantTime[player] = (float)Server.CurrentTime + _config.Dices.ImposterSyndrome.DecoyInterval;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextCheckTime.Remove(player);
            _ = _nextDecoyGrantTime.Remove(player);
            CleanupDecoyGlows(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                CleanupDecoyGlows(p);
            _players.Clear();
            _nextCheckTime.Clear();
            _nextDecoyGrantTime.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        private void CleanupDecoyGlows(CCSPlayerController player)
        {
            if (_decoyGlows.TryGetValue(player, out var glows))
            {
                foreach (var (proxy, glow) in glows)
                    GlowUtil.RemoveGlow(proxy, glow);
                _decoyGlows.Remove(player);
            }
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (entity.DesignerName != "decoy_projectile") return;

            Server.NextFrame(() =>
            {
                if (entity == null || !entity.IsValid) return;
                var decoy = new CDecoyProjectile(entity.Handle);
                if (!decoy.IsValid) return;

                var throwerPawn = decoy.Thrower?.Value;
                if (throwerPawn == null || !throwerPawn.IsValid) return;
                var thrower = throwerPawn.Controller?.Value?.As<CCSPlayerController>();
                if (thrower == null || !thrower.IsValid || !_players.Contains(thrower)) return;

                // Decoy thrown — glow all alive enemies for 1 second
                var enemyGlows = new List<(CDynamicProp?, CDynamicProp?)>();
                foreach (var enemy in Utilities.GetPlayers())
                {
                    if (enemy == null || !enemy.IsValid
                        || enemy == thrower
                        || enemy.TeamNum == thrower.TeamNum
                        || enemy.PlayerPawn?.Value == null
                        || !enemy.PlayerPawn.Value.IsValid
                        || enemy.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    var glow = GlowUtil.CreateGlow(enemy.PlayerPawn.Value, Color.Orange);
                    enemyGlows.Add(glow);
                }

                if (enemyGlows.Count > 0)
                {
                    _decoyGlows[thrower] = enemyGlows;
                    thrower.PrintToCenterAlert("👁 诱饵弹暴露了敌人位置!");
                }

                // Remove glows after 1 second
                var capturedThrower = thrower;
                new CounterStrikeSharp.API.Modules.Timers.Timer(1f, () =>
                {
                    CleanupDecoyGlows(capturedThrower);
                });
            });
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            foreach (CCSPlayerController player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    // Grant free decoy grenade every interval
                    if (_nextDecoyGrantTime.TryGetValue(player, out float grantTime) && now >= grantTime)
                    {
                        CCSPlayerPawn p = player.PlayerPawn.Value;
                        // Only grant if player doesn't already have a decoy equipped
                        bool hasDecoy = false;
                        var weapons = p.WeaponServices?.MyWeapons;
                        if (weapons != null)
                        {
                            foreach (var wh in weapons)
                            {
                                if (wh.Value?.DesignerName == "weapon_decoy")
                                {
                                    hasDecoy = true;
                                    break;
                                }
                            }
                        }
                        if (!hasDecoy)
                        {
                            player.GiveNamedItem("weapon_decoy");
                            player.PrintToCenterAlert("🎯 第六感：获得诱饵弹！");
                        }
                        _nextDecoyGrantTime[player] = now + _config.Dices.ImposterSyndrome.DecoyInterval;
                    }

                    // Radar detection check
                    if (_nextCheckTime.TryGetValue(player, out float next) && next <= now)
                    {
                        _nextCheckTime[player] = now + 1f;

                        CCSPlayerPawn pawn = player.PlayerPawn.Value;
                        if (pawn.EntitySpottedState is { Spotted: true })
                        {
                            player.PrintToCenterAlert("📍 你被雷达发现了!");
                            player.EmitSound("UI.PlayerPingUrgent");
                            _nextCheckTime[player] = now + 5f;
                        }
                    }
                }
                catch
                {
                    _nextCheckTime.Remove(player);
                    _nextDecoyGrantTime.Remove(player);
                }
            }
        }
    }
}
