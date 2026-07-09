using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Plague : DiceBlueprint
    {
        public override string ClassName => "Plague";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        // Static: all infected players across all instances
        public static readonly HashSet<ulong> InfectedPlayers = [];
        private float _lastDamageTime;

        public Plague(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Karma") || DiceSynergy.HasPartner(player, "Parasite");
            if (DiceSynergy.HasPartner(player, "Karma"))
                DiceSynergy.AnnounceCombo(player, "因果循环", "双倍感染率");
            if (DiceSynergy.HasPartner(player, "Parasite"))
                DiceSynergy.AnnounceCombo(player, "生化危机", "瘟疫+寄生！传染翻倍+寄生效果翻倍！");
            InfectedPlayers.Add(player.SteamID);

            player.PrintToCenterAlert("🦠 你感染了瘟疫！每秒-1HP，攻击可传染！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            InfectedPlayers.Clear();
            _lastDamageTime = 0;
        }

        public override void Destroy() => Reset();

        // Transmission: infected attacker → victim gets infected
        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (InfectedPlayers.Count == 0) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !InfectedPlayers.Contains(attacker.SteamID))
                return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || attacker == victim)
                return HookResult.Continue;

            // Don't infect teammates (unless you want to spread it to everyone)
            if (InfectedPlayers.Contains(victim.SteamID))
                return HookResult.Continue;

            InfectedPlayers.Add(victim.SteamID);
            victim.PrintToCenterAlert("🦠 你被瘟疫传染了！");
            victim.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_Plague_infected"].Value}");

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Plague_spread"].Value.Replace("{attacker}", attacker.PlayerName).Replace("{victim}", victim.PlayerName)}");

            return HookResult.Continue;
        }

        // Tick: damage all infected players every 1 second
        public void OnTick()
        {
            if (InfectedPlayers.Count == 0) return;

            float now = (float)Server.CurrentTime;
            if (now - _lastDamageTime < 1f) return;
            _lastDamageTime = now;

            int dmg = _config.Dices.Plague.DamagePerSecond;

            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && InfectedPlayers.Contains(p.SteamID)
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                pawn.Health -= dmg;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                if (pawn.Health <= 0)
                {
                    // Remove from infected set on death
                    InfectedPlayers.Remove(p.SteamID);
                    if (!p.IsBot)
                        pawn.CommitSuicide(false, true);
                }
            }

            // Clean up dead/disconnected from infected set
            InfectedPlayers.RemoveWhere(steamId =>
            {
                var player = Utilities.GetPlayers().FirstOrDefault(pl => pl.SteamID == steamId);
                return player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE;
            });
        }
    }
}
