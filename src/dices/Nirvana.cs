using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Nirvana : DiceBlueprint
    {
        public override string ClassName => "Nirvana";
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private bool _comboActive;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, float> _cooldowns = [];

        // Spawn entities
        private CBaseEntity[] _playerSpawnEntities = [];
        private CBaseEntity[] _ctSpawnEntities = [];
        private CBaseEntity[] _tSpawnEntities = [];

        public Nirvana(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _cooldowns[player] = 0f;
            _comboActive = DiceSynergy.HasPartner(player, "GuardianAngel");
            if (_comboActive)
            {
                var instance = RollTheDice.Instance;
                if (instance != null && instance.HasDiceActive(player, "GuardianAngel"))
                {
                    DiceSynergy.AnnounceCombo(player, "菲尼克斯", "涅槃+守护天使合成为菲尼克斯！");
                    var captured = player;
                    Server.NextFrame(() =>
                    {
                        if (instance != null && captured.IsValid)
                        {
                            instance.RemoveDiceFromPlayer(captured, "Nirvana");
                            instance.RemoveDiceFromPlayer(captured, "GuardianAngel");
                            instance.ForceDiceForPlayer(captured, "Phoenix");
                        }
                    });
                    return;
                }
                else
                    DiceSynergy.AnnounceCombo(player, "菲尼克斯", "团队联动！涅槃与守护天使共鸣！");
            }
            FindSpawnPoints();
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

        public override void Destroy() => Reset();

        private void FindSpawnPoints()
        {
            if (_ctSpawnEntities.Length > 0 || _tSpawnEntities.Length > 0) return;
            _playerSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist"))
                .ToArray();
            _ctSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist").ToArray();
            _tSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist").ToArray();
        }

        private Vector? GetSpawnPos(CCSPlayerController player)
        {
            CBaseEntity[] teamSpawns = player.Team == CsTeam.CounterTerrorist ? _ctSpawnEntities : _tSpawnEntities;
            var allSpawns = teamSpawns.Concat(_playerSpawnEntities).OrderBy(_ => _random.Next()).ToList();
            foreach (var s in allSpawns)
            {
                if (s?.AbsOrigin != null)
                    return s.AbsOrigin;
            }
            return null;
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            if (info.Damage <= 0) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            if (_cooldowns.TryGetValue(victim, out float cd) && now < cd) return HookResult.Continue;

            // Roll chance: 30-50%
            float min = _config.Dices.Nirvana.MinChance;
            float max = _config.Dices.Nirvana.MaxChance;
            float threshold = min + (float)(_random.NextDouble() * (max - min));
            if (_random.NextDouble() > threshold) return HookResult.Continue;

            var spawnPos = GetSpawnPos(victim);
            if (spawnPos == null) return HookResult.Continue;

            // Cooldown to prevent multiple triggers
            _cooldowns[victim] = now + _config.Dices.Nirvana.Cooldown;

            string playerName = victim.PlayerName;

            // Teleport to spawn, heal, fix armor
            Server.NextFrame(() =>
            {
                if (victim?.PlayerPawn?.Value == null || !victim.PlayerPawn.Value.IsValid) return;
                CCSPlayerPawn pawn = victim.PlayerPawn.Value;

                pawn.Teleport(spawnPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));

                pawn.Health = pawn.MaxHealth;
                pawn.ArmorValue = 100;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

                victim.PrintToCenterAlert("🌸 彼岸花开！回到出生点！");
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Nirvana_broadcast"].Value.Replace("{playerName}", playerName)}");
            });

            return HookResult.Continue;
        }
    }
}
