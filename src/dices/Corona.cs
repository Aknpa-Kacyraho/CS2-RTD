using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Corona : DiceBlueprint
    {
        public override string ClassName => "Corona";
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnPlayerTakeDamagePre", "OnTick"];

        private float _roundStartTime;
        private readonly Dictionary<CCSPlayerController, bool> _coronated = [];
        private readonly Dictionary<ulong, (float EndTime, float LastTick)> _fireTargets = [];

        public Corona(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _coronated[player] = false;
            if (_roundStartTime == 0) _roundStartTime = (float)Server.CurrentTime;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player); _ = _coronated.Remove(player);
        }

        public override void Reset() { _players.Clear(); _coronated.Clear(); _roundStartTime = 0; _fireTargets.Clear(); }
        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid || !_players.Contains(victim)) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            if (now - _roundStartTime < _config.Dices.Corona.Delay) return HookResult.Continue;
            if (_coronated.TryGetValue(victim, out bool done) && done) return HookResult.Continue;

            CCSPlayerPawn pawn = victim.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;

            string weapon = @event.Weapon ?? "";
            bool isFire = weapon.Contains("inferno", StringComparison.OrdinalIgnoreCase)
                       || weapon.Contains("molotov", StringComparison.OrdinalIgnoreCase)
                       || weapon.Contains("hegrenade", StringComparison.OrdinalIgnoreCase);
            if (!isFire) return HookResult.Continue;

            Server.NextFrame(() =>
            {
                if (victim == null || !victim.IsValid) return;
                victim.Respawn();
                Server.NextFrame(() =>
                {
                    Server.NextFrame(() =>
                    {
                        if (victim.PlayerPawn?.Value is not CCSPlayerPawn rPawn || !rPawn.IsValid) return;
                        rPawn.MaxHealth = _config.Dices.Corona.RespawnHP;
                        rPawn.Health = _config.Dices.Corona.RespawnHP;
                        rPawn.ArmorValue = _config.Dices.Corona.RespawnArmor;
                        Utilities.SetStateChanged(rPawn, "CBaseEntity", "m_iMaxHealth");
                        Utilities.SetStateChanged(rPawn, "CBaseEntity", "m_iHealth");
                        Utilities.SetStateChanged(rPawn, "CCSPlayerPawn", "m_ArmorValue");
                        _coronated[victim] = true;
                        victim.PrintToCenterAlert("☀️ 加冕太阳神！200HP+火焰附伤！");
                        Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☀️ {victim.PlayerName} 从火焰中重生，加冕为太阳神！");
                    });
                });
            });

            return HookResult.Continue;
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_coronated.TryGetValue(attacker, out bool done) || !done)
                return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || victim.SteamID == attacker.SteamID) return HookResult.Continue;

            float now = (float)Server.CurrentTime;
            _fireTargets[victim.SteamID] = (now + _config.Dices.Corona.FireDuration, now);
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_fireTargets.Count == 0) return;
            float now = (float)Server.CurrentTime;

            foreach (var (steamId, (endTime, lastTick)) in _fireTargets.ToList())
            {
                if (now >= endTime) { _fireTargets.Remove(steamId); continue; }
                if (now - lastTick < 1f) continue;

                _fireTargets[steamId] = (endTime, now);

                var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
                if (player?.PlayerPawn?.Value is not CCSPlayerPawn pawn || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                { _fireTargets.Remove(steamId); continue; }

                pawn.Health -= _config.Dices.Corona.FireDps;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                if (pawn.Health <= 0)
                {
                    if (!player.IsBot && !player.IsHLTV)
                        pawn.CommitSuicide(false, true);
                    else
                    {
                        try { pawn.CommitSuicide(false, true); }
                        catch { pawn.Health = 0; Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth"); }
                    }
                }
            }
        }
    }
}
