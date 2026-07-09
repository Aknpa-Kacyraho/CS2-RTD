using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Awakener : DiceBlueprint
    {
        public override string ClassName => "Awakener";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, int> _killCount = [];

        public Awakener(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Evolution"); _killCount[player] = _comboActive ? 1 : 0;
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "超进化", "超进化联动生效！");
            _killCount[player] = 0;
            ApplyStats(player, 0);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("⚡ 觉醒者！击杀2人以觉醒全部力量...");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            Revert(player);
            _ = _players.Remove(player);
            _ = _killCount.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Revert(p);
            _players.Clear(); _killCount.Clear();
        }

        private void Revert(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        private void ApplyStats(CCSPlayerController player, int kills)
        {
            if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            int max = _config.Dices.Awakener.KillsToMax;
            float t = Math.Min((float)kills / max, 1f);
            float spdMult = _config.Dices.Awakener.StartSpeedMult + t * (_config.Dices.Awakener.MaxSpeedMult - _config.Dices.Awakener.StartSpeedMult);

            player.PlayerPawn.Value.VelocityModifier = spdMult;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        private float GetDamageMult(CCSPlayerController player)
        {
            int kills = _killCount.TryGetValue(player, out int k) ? k : 0;
            int max = _config.Dices.Awakener.KillsToMax;
            float t = Math.Min((float)kills / max, 1f);
            return _config.Dices.Awakener.StartDamageMult + t * (_config.Dices.Awakener.MaxDamageMult - _config.Dices.Awakener.StartDamageMult);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (attacker == @event.Userid) return HookResult.Continue;

            int current = _killCount.TryGetValue(attacker, out int k) ? k : 0;
            current++;
            _killCount[attacker] = current;
            ApplyStats(attacker, current);

            int max = _config.Dices.Awakener.KillsToMax;
            if (current == 1)
                attacker.PrintToCenterAlert($"⚡ 觉醒中... ({current}/{max})");
            else if (current >= max)
                attacker.PrintToCenterAlert("⚡ 觉醒完成！伤害×2 速度×1.5！");
            else
                attacker.PrintToCenterAlert($"⚡ 觉醒中... ({current}/{max})");

            return HookResult.Continue;
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            info.Damage *= GetDamageMult(attacker);
            return HookResult.Changed;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                    if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    int kills = _killCount.TryGetValue(player, out int k) ? k : 0;
                    ApplyStats(player, kills);
                }
                catch { }
            }
        }
    }
}
