using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Afterimage : DiceBlueprint
    {
        public override string ClassName => "Afterimage";
        public override List<string> Listeners => [
            "OnTick",
            "OnPlayerButtonsChanged"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, List<(Vector Pos, QAngle Angles, Vector Velocity)>> _shadows = [];
        private readonly Dictionary<CCSPlayerController, float> _nextRecordTime = [];
        private readonly Dictionary<CCSPlayerController, float> _recallCooldowns = [];

        public Afterimage(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _shadows[player] = [];
            _nextRecordTime[player] = (float)Server.CurrentTime + 1.5f; // first shadow soon
            _recallCooldowns[player] = 0f;

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _shadows.Remove(player);
            _ = _nextRecordTime.Remove(player);
            _ = _recallCooldowns.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _shadows.Clear();
            _nextRecordTime.Clear();
            _recallCooldowns.Clear();
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid) continue;
                    if (!_nextRecordTime.TryGetValue(player, out float nextRecord) || now < nextRecord) continue;
                    if (player.PlayerPawn?.Value?.AbsOrigin == null) continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                    // Record shadow
                    Vector pos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
                    QAngle angles = new(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z);
                    Vector vel = pawn.AbsVelocity != null
                        ? new Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z)
                        : new Vector(0, 0, 0);

                    _shadows[player].Add((pos, angles, vel));

                    // Trim to max
                    int max = _config.Dices.Afterimage.MaxShadows;
                    while (_shadows[player].Count > max)
                        _shadows[player].RemoveAt(0);

                    // Next record time
                    float interval = _config.Dices.Afterimage.RecordIntervalMin +
                        (float)(_random.NextDouble() * (_config.Dices.Afterimage.RecordIntervalMax - _config.Dices.Afterimage.RecordIntervalMin));
                    _nextRecordTime[player] = now + interval;
                }
                catch { }
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return; // E key
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            // Check cooldown
            float now = (float)Server.CurrentTime;
            if (_recallCooldowns.TryGetValue(player, out float cd) && now < cd) return;

            // Check shadows exist
            if (!_shadows.TryGetValue(player, out var shadowList) || shadowList.Count == 0) return;

            // Pop most recent shadow (last element)
            var shadow = shadowList[^1];
            shadowList.RemoveAt(shadowList.Count - 1);

            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            // Smoke at departure
            if (_config.Dices.Afterimage.SmokeOnRecall && pawn.AbsOrigin != null)
            {
                SpawnPuffSmoke(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 5);
            }

            // Teleport to shadow
            pawn.Teleport(shadow.Pos, shadow.Angles, shadow.Velocity);

            // Smoke at arrival
            if (_config.Dices.Afterimage.SmokeOnRecall)
            {
                SpawnPuffSmoke(shadow.Pos.X, shadow.Pos.Y, shadow.Pos.Z + 5);
            }

            _recallCooldowns[player] = now + _config.Dices.Afterimage.RecallCooldown;
            player.PrintToCenterAlert("🌀 时空回溯!");
        }

        private static void SpawnPuffSmoke(float x, float y, float z)
        {
            Vector pos = new(x, y, z);
            var smoke = Utilities.CreateEntityByName<CSmokeGrenadeProjectile>("smokegrenade_projectile");
            if (smoke != null && smoke.IsValid)
            {
                smoke.Teleport(pos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                smoke.DispatchSpawn();
                smoke.SmokeColor.X = 150;
                smoke.SmokeColor.Y = 150;
                smoke.SmokeColor.Z = 220;
                smoke.DetonateTime = 0f;
                smoke.AcceptInput("InitializeSpawnFromWorld");
                smoke.AcceptInput("Detonate");
                Server.NextFrame(() =>
                {
                    if (smoke != null && smoke.IsValid)
                    {
                        smoke.DetonateTime = 0f;
                        smoke.AcceptInput("Detonate");
                    }
                });
            }
        }
    }
}
