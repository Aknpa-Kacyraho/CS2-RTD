using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class RouletteGambler : DiceBlueprint
    {
        public override string ClassName => "RouletteGambler";
        public override List<string> Events => ["EventPlayerDeath", "EventWeaponFire"];
        public override List<string> Listeners => [
            "OnTick",
            "OnPlayerTakeDamagePre"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly Dictionary<CCSPlayerController, int> _bonusShots = [];

        public RouletteGambler(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);

            _bonusShots[player] = 0;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            RevertBonus(player);
            _ = _bonusShots.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                RevertBonus(p);
            }
            _players.Clear();
            _bonusShots.Clear();
        }

        public override void Destroy() => Reset();

        private void RevertBonus(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        private void ApplyBonus(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            int shots = _bonusShots.TryGetValue(player, out int s) ? s : 0;
            float maxPercent = _config.Dices.RouletteGambler.BonusMaxPercent;
            float bonus = Math.Min(shots * 0.01f, maxPercent / 100f);
            player.PlayerPawn.Value.VelocityModifier = 1.0f + bonus;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        public void OnTick()
        {
            if (_bonusShots.Count == 0) return;
            foreach (var kvp in _bonusShots.ToList())
            {
                try
                {
                    if (kvp.Key?.PlayerPawn?.Value == null || !kvp.Key.PlayerPawn.Value.IsValid) continue;
                    if (kvp.Key.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    ApplyBonus(kvp.Key);
                }
                catch { }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_bonusShots.Count == 0) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            int shots = _bonusShots.TryGetValue(attacker, out int s) ? s : 0;
            float maxPercent = _config.Dices.RouletteGambler.BonusMaxPercent;
            float bonus = Math.Min(shots * 0.01f, maxPercent / 100f);
            info.Damage *= (1.0f + bonus);
            return HookResult.Changed;
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || !_players.Contains(player)
                || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return HookResult.Continue;

            float deathChance = _config.Dices.RouletteGambler.DeathChance;
            if (_random.NextDouble() <= deathChance)
            {
                if (!player.IsBot)
                    player.PlayerPawn.Value.CommitSuicide(false, true);
                player.PrintToCenterAlert("🔫 赌命失败!");
                return HookResult.Continue;
            }

            // Increment bonus
            int currentShots = _bonusShots.TryGetValue(player, out int s) ? s : 0;
            int maxShots = (int)(_config.Dices.RouletteGambler.BonusMaxPercent);
            if (currentShots < maxShots)
            {
                _bonusShots[player] = currentShots + 1;
                ApplyBonus(player);
                player.PrintToCenterAlert($"🔫 赌命成功! 当前加成: {currentShots + 1}%");
            }

            return HookResult.Continue;
        }
    }
}
