using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class DecoyDummy : DiceBlueprint
    {
        public override string ClassName => "DecoyDummy";
        public override List<string> Listeners => ["OnTick", "OnEntitySpawned"];
        private readonly Dictionary<CCSPlayerController, float> _nextGrenadeTime = [];
        private readonly Dictionary<CCSPlayerController, List<CDynamicProp>> _activeDummies = [];

        public DecoyDummy(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _nextGrenadeTime[player] = (float)Server.CurrentTime + _config.Dices.DecoyDummy.GrenadeInterval;

            player.GiveNamedItem("weapon_decoy");
            player.PrintToCenterAlert("🪆 获得诱饵弹！每20秒补一颗！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextGrenadeTime.Remove(player);
            CleanupDummies(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                CleanupDummies(p);
            _players.Clear();
            _nextGrenadeTime.Clear();
        }

        public override void Destroy() => Reset();

        private void CleanupDummies(CCSPlayerController player)
        {
            if (_activeDummies.TryGetValue(player, out var dummies))
            {
                foreach (var d in dummies)
                {
                    if (d != null && d.IsValid) d.Remove();
                }
                _activeDummies.Remove(player);
            }
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (entity.DesignerName != "decoy_projectile") return;

            // Capture position NOW before decoy expires
            Vector? decoyPos = null;
            CCSPlayerController? thrower = null;

            Server.NextFrame(() =>
            {
                if (entity == null || !entity.IsValid) return;
                var decoy = new CDecoyProjectile(entity.Handle);
                if (!decoy.IsValid || decoy.AbsOrigin == null) return;

                var throwerPawn = decoy.Thrower?.Value;
                if (throwerPawn == null || !throwerPawn.IsValid) return;
                thrower = throwerPawn.Controller?.Value?.As<CCSPlayerController>();
                if (thrower == null || !thrower.IsValid || !_players.Contains(thrower)) return;

                decoyPos = new Vector(decoy.AbsOrigin.X, decoy.AbsOrigin.Y, decoy.AbsOrigin.Z + 72f);

                // Spawn dummy after delay (let it land naturally)
                var captured = thrower;
                var pos = decoyPos;
                new CounterStrikeSharp.API.Modules.Timers.Timer(1.5f, () =>
                {
                    if (captured == null || !captured.IsValid) return;
                    if (pos == null) return;

                    var dummy = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
                    if (dummy == null) return;
                    dummy.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
                    dummy.Render = Color.FromArgb(255, 200, 200, 200);
                    dummy.Teleport(pos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    dummy.DispatchSpawn();

                    if (!_activeDummies.ContainsKey(captured))
                        _activeDummies[captured] = [];
                    _activeDummies[captured].Add(dummy);

                    captured.PrintToCenterAlert("🪆 假人诱饵已部署！");

                    var capturedDummy = dummy;
                    new CounterStrikeSharp.API.Modules.Timers.Timer(8f, () =>
                    {
                        if (capturedDummy != null && capturedDummy.IsValid)
                            capturedDummy.Remove();
                        if (_activeDummies.TryGetValue(captured, out var list))
                            list.Remove(capturedDummy);
                    });
                });
            });
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                if (_nextGrenadeTime.TryGetValue(player, out float next) && now >= next)
                {
                    _nextGrenadeTime[player] = now + _config.Dices.DecoyDummy.GrenadeInterval;
                    player.GiveNamedItem("weapon_decoy");
                    player.PrintToCenterAlert("🪆 补给了一颗诱饵弹！");
                }
            }
        }
    }
}
