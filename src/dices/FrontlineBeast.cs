using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class FrontlineBeast : DiceBlueprint
    {
        public override string ClassName => "FrontlineBeast";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick"];

        // Static to survive mp_backup_restore_load_file re-instantiations
        // SteamID → kill count since getting this dice
        public static readonly Dictionary<ulong, int> BeastKills = [];

        private float BaseSpeed => _config.Dices.FrontlineBeast.SpeedMult;
        private float PerKill => _config.Dices.FrontlineBeast.SpeedMultPerKill;
        private float MaxSpeed => _config.Dices.FrontlineBeast.SpeedMultMax;

        public FrontlineBeast(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "SpeedOnKill");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "猎杀本能", "前线速度加倍 猎杀时限翻倍");

            // Initialize kill count and apply base speed
            if (!BeastKills.ContainsKey(player.SteamID))
                BeastKills[player.SteamID] = 0;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.VelocityModifier = BaseSpeed;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert($"🦁 前线巨兽！速度×{BaseSpeed}，每次击杀+{PerKill}，最高×{MaxSpeed}！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (reason != DiceRemoveReason.NewDice)
            {
                BeastKills.Remove(player.SteamID);
            }
        }

        public override void Reset()
        {
            _players.Clear();
            BeastKills.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid || attacker == victim)
                return HookResult.Continue;

            // No friendly fire trigger
            if (attacker.TeamNum == victim.TeamNum) return HookResult.Continue;

            CCSPlayerPawn pawn = attacker.PlayerPawn!.Value!;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;

            // Increment kill count and compute new speed
            if (!BeastKills.TryGetValue(attacker.SteamID, out int kills))
                kills = 0;
            kills++;
            BeastKills[attacker.SteamID] = kills;

            float speedMult = Math.Min(BaseSpeed + kills * PerKill, MaxSpeed);
            if (_comboActive) speedMult = Math.Min(speedMult * 2f, MaxSpeed * 2f);

            // Teleport to spawn
            var spawnPoints = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist"))
                .Where(s => s.IsValid && s.AbsOrigin != null)
                .ToList();

            if (spawnPoints.Count > 0)
            {
                var spawnsForTeam = spawnPoints
                    .Where(s => (attacker.TeamNum == 2 && s.DesignerName.Contains("terrorist"))
                        || (attacker.TeamNum == 3 && s.DesignerName.Contains("counterterrorist")))
                    .ToList();

                if (spawnsForTeam.Count == 0)
                    spawnsForTeam = spawnPoints;

                var random = new Random(Guid.NewGuid().GetHashCode());
                CBaseEntity chosenSpawn = spawnsForTeam[random.Next(spawnsForTeam.Count)];
                pawn.Teleport(chosenSpawn.AbsOrigin, chosenSpawn.AbsRotation, new Vector(0, 0, 0));
            }

            // Full HP restore
            pawn.Health = pawn.MaxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            // Apply new speed
            pawn.VelocityModifier = speedMult;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            attacker.PrintToCenterAlert($"🦁 前线巨兽！{kills}杀 速度×{speedMult:F1} 满血！");

            return HookResult.Continue;
        }

        // Maintain VelocityModifier (engine resets it on hit/weapon switch etc.)
        public void OnTick()
        {
            if (BeastKills.Count == 0 && _players.Count == 0) return;

            float baseSpeed = BaseSpeed;
            float perKill = PerKill;
            float maxSpeed = MaxSpeed;
            bool combo = _comboActive;

            // Maintain speed for players who have kills
            foreach (var kv in BeastKills.ToList())
            {
                var player = Utilities.GetPlayers()
                    .FirstOrDefault(p => p.IsValid && !p.IsHLTV && p.SteamID == kv.Key);
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    continue;
                }

                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                float target = Math.Min(baseSpeed + kv.Value * perKill, maxSpeed);
                if (combo) target = Math.Min(target * 2f, maxSpeed * 2f);

                if (Math.Abs(pawn.VelocityModifier - target) > 0.01f)
                {
                    pawn.VelocityModifier = target;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }

            // Also maintain base speed for players who just got the dice (no kills yet)
            foreach (var p in _players.ToList())
            {
                if (p == null || !p.IsValid) continue;
                if (BeastKills.ContainsKey(p.SteamID)) continue; // already handled above

                if (p.PlayerPawn?.Value == null || !p.PlayerPawn.Value.IsValid
                    || p.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                CCSPlayerPawn pawn = p.PlayerPawn.Value;
                if (Math.Abs(pawn.VelocityModifier - baseSpeed) > 0.01f)
                {
                    pawn.VelocityModifier = baseSpeed;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
