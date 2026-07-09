using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class SlyFox : DiceBlueprint
    {
        public override string ClassName => "SlyFox";
        public override List<string> Listeners => ["OnEntitySpawned", "OnTick"];

        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private float _nextItemTime;

        private readonly HashSet<string> _grenadeProjectiles =
        [
            "smokegrenade_projectile", "hegrenade_projectile",
            "molotov_projectile", "decoy_projectile", "flashbang_projectile"
        ];

        private static readonly string[] RandomItems = [
            "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade",
            "weapon_molotov", "weapon_incgrenade", "weapon_decoy"
        ];

        public SlyFox(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
        { _ = _players.Remove(player); }

        public override void Reset() { _players.Clear(); _nextItemTime = 0; }
        public override void Destroy() => Reset();

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0) return;
            if (!_grenadeProjectiles.Contains(entity.DesignerName)) return;

            Server.NextFrame(() =>
            {
                if (entity.Handle == IntPtr.Zero) return;
                CBaseGrenade grenade = new(entity.Handle);
                if (!grenade.IsValid || grenade.Handle == IntPtr.Zero) return;
                CCSPlayerPawn? owner = grenade.OriginalThrower?.Value;
                if (owner == null || !owner.IsValid) return;
                CCSPlayerController? thrower = owner.Controller?.Value?.As<CCSPlayerController>();
                if (thrower == null || !thrower.IsValid || !_players.Contains(thrower)) return;

                float delay = 1f + (float)(_random.NextDouble() * 19f); // random 1-20 seconds
                grenade.DetonateTime = Server.CurrentTime + delay;
            });
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;
            if (now < _nextItemTime) return;
            _nextItemTime = now + 10f;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid) continue;
                if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                string item = RandomItems[_random.Next(RandomItems.Length)];
                player.GiveNamedItem(item);
                player.PrintToCenterAlert($"🦊 狡猾狐狸获得了随机道具！");
            }
        }
    }
}
