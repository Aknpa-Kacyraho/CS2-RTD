using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class WASDChaos : DiceBlueprint
    {
        public override string ClassName => "WASDChaos";
        public override List<string> Listeners => ["OnTick", "OnPlayerButtonsChanged"];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        // Track which buttons each player is currently pressing
        private readonly Dictionary<CCSPlayerController, PlayerButtons> _heldButtons = [];
        // Chaos angle offset per player (deg): 90=W→D, 180=W→S, -90=W→A
        private readonly Dictionary<CCSPlayerController, float> _chaosOffset = [];
        // When to next reshuffle each player's chaos offset
        private readonly Dictionary<CCSPlayerController, float> _nextReshuffle = [];

        private static readonly float[] ChaosPresets = [90f, -90f, 180f, 45f, -45f, 135f, -135f, 0f];

        public WASDChaos(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            _heldButtons[player] = 0;
            _chaosOffset[player] = ChaosPresets[_random.Next(ChaosPresets.Length)];
            _nextReshuffle[player] = (float)Server.CurrentTime + 2f + (float)(_random.NextDouble() * 3f);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_WASDChaos_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _heldButtons.Remove(player);
            _ = _chaosOffset.Remove(player);
            _ = _nextReshuffle.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _heldButtons.Clear();
            _chaosOffset.Clear();
            _nextReshuffle.Clear();
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid) return;

            // Track pressed movement buttons — auto-register any alive player
            if (!_heldButtons.TryGetValue(player, out var held)) held = 0;

            var moveButtons = PlayerButtons.Forward | PlayerButtons.Back | PlayerButtons.Moveleft | PlayerButtons.Moveright;

            // Add newly pressed, remove released
            held |= pressed & moveButtons;
            held &= ~(released & moveButtons);

            _heldButtons[player] = held;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var player in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                try
                {
                    CCSPlayerPawn pawn = player.PlayerPawn!.Value!;

                    // Auto-register any alive player
                    if (!_chaosOffset.ContainsKey(player))
                    {
                        _chaosOffset[player] = ChaosPresets[_random.Next(ChaosPresets.Length)];
                        _heldButtons[player] = 0;
                        _nextReshuffle[player] = now + 2f + (float)(_random.NextDouble() * 3f);
                    }

                    // Reshuffle chaos offset periodically
                    if (_nextReshuffle.TryGetValue(player, out float next) && now >= next)
                    {
                        float newOffset = ChaosPresets[_random.Next(ChaosPresets.Length)];
                        _chaosOffset[player] = newOffset;
                        float interval = _config.Dices.WASDChaos.MinInterval +
                            (float)(_random.NextDouble() * (_config.Dices.WASDChaos.MaxInterval - _config.Dices.WASDChaos.MinInterval));
                        _nextReshuffle[player] = now + interval;

                        // Notify the player their controls shifted
                        string dir = newOffset switch
                        {
                            90f => "W→A / A→S / S→D / D→W",
                            -90f => "W→D / D→S / S→A / A→W",
                            180f => "W↔S / A↔D",
                            45f => "45°旋转",
                            _ => "方向重新映射!"
                        };
                        player.PrintToCenterAlert($"🌀 {dir}");
                    }

                    // Apply chaos: redirect velocity based on held buttons + chaos offset
                    var held = _heldButtons.TryGetValue(player, out var h) ? h : player.Buttons;
                    if ((held & (PlayerButtons.Forward | PlayerButtons.Back | PlayerButtons.Moveleft | PlayerButtons.Moveright)) == 0)
                        continue;

                    // Get player's actual facing direction from eye angles
                    float baseYaw = pawn.EyeAngles.Y;
                    float chaosYaw = baseYaw + _chaosOffset.GetValueOrDefault(player, 90f);
                    float yawRad = chaosYaw * MathF.PI / 180f;

                    Vector wishDir = new(0, 0, 0);

                    if (held.HasFlag(PlayerButtons.Forward))
                    {
                        wishDir.X += MathF.Cos(yawRad);
                        wishDir.Y += MathF.Sin(yawRad);
                    }
                    if (held.HasFlag(PlayerButtons.Back))
                    {
                        wishDir.X -= MathF.Cos(yawRad);
                        wishDir.Y -= MathF.Sin(yawRad);
                    }
                    if (held.HasFlag(PlayerButtons.Moveleft))
                    {
                        wishDir.X += MathF.Sin(yawRad);
                        wishDir.Y -= MathF.Cos(yawRad);
                    }
                    if (held.HasFlag(PlayerButtons.Moveright))
                    {
                        wishDir.X -= MathF.Sin(yawRad);
                        wishDir.Y += MathF.Cos(yawRad);
                    }

                    if (wishDir.X != 0 || wishDir.Y != 0)
                    {
                        float len = MathF.Sqrt(wishDir.X * wishDir.X + wishDir.Y * wishDir.Y);
                        float speed = 260f; // CS2 default run speed
                        wishDir.X = wishDir.X / len * speed;
                        wishDir.Y = wishDir.Y / len * speed;

                        float vz = pawn.AbsVelocity?.Z ?? 0f;
                        pawn.Teleport(null, null, new Vector(wishDir.X, wishDir.Y, vz));
                    }
                }
                catch { }
            }
        }
    }
}
