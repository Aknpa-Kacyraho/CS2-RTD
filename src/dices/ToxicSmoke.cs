using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ToxicSmoke : DiceBlueprint
    {
        public override string ClassName => "ToxicSmoke";
        public override List<string> Listeners => ["OnEntitySpawned", "OnTick"];
        private readonly HashSet<nint> _toxicSmokes = [];
        private readonly Dictionary<nint, CCSPlayerController> _smokeOwners = [];
        private float _lastDamageTime;

        public ToxicSmoke(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _toxicSmokes.Clear();
            _smokeOwners.Clear();
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (entity.DesignerName != "smokegrenade_projectile") return;

            Server.NextFrame(() =>
            {
                if (!entity.IsValid) return;
                var smoke = new CSmokeGrenadeProjectile(entity.Handle);
                if (!smoke.IsValid) return;

                var thrower = smoke.OriginalThrower?.Value;
                if (thrower == null || !thrower.IsValid) return;

                var controller = thrower.Controller?.Value?.As<CCSPlayerController>();
                if (controller == null || !controller.IsValid || !_players.Contains(controller)) return;

                _toxicSmokes.Add(smoke.Handle);
                _smokeOwners[smoke.Handle] = controller;
            });
        }

        public void OnTick()
        {
            if (_toxicSmokes.Count == 0) return;
            float now = (float)Server.CurrentTime;
            if (now - _lastDamageTime < 1.0f) return;
            _lastDamageTime = now;

            // Prune dead smokes
            var deadSmokes = _toxicSmokes.Where(h => !new CSmokeGrenadeProjectile(h).IsValid).ToList();
            foreach (var h in deadSmokes)
            {
                _toxicSmokes.Remove(h);
                _smokeOwners.Remove(h);
            }

            int dmg = _config.Dices.ToxicSmoke.DamagePerSecond;
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                    continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                var pawn = player.PlayerPawn.Value;
                var pos = pawn.AbsOrigin;
                if (pos == null) continue;

                // Check if player is inside any toxic smoke
                foreach (var smokeHandle in _toxicSmokes.ToList())
                {
                    var smoke = new CSmokeGrenadeProjectile(smokeHandle);
                    if (!smoke.IsValid || smoke.AbsOrigin == null) continue;

                    float dist = Vectors.GetDistance(pos, smoke.AbsOrigin);
                    if (dist <= 200f) // smoke radius
                    {
                        pawn.Health -= dmg;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        if (pawn.Health <= 0)
                        {
                            if (!player.IsBot)
                                pawn.CommitSuicide(false, true);
                        }
                    }
                }
            }
        }
    }
}
