using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Karma : DiceBlueprint
    {
        public override string ClassName => "Karma";
        private bool _comboActive;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick"];

        // Static: all players with the karma buff (speed + regen)
        public static readonly HashSet<ulong> BuffedPlayers = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private float _lastHealTime;

        public Karma(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Plague");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "因果循环", "双倍回血 双倍感染");

            // Give buff to holder
            BuffedPlayers.Add(player.SteamID);

            // Apply speed
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.VelocityModifier = _config.Dices.Karma.SpeedMultiplier;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("☸ 因果报应！移速×1.5 + 每秒回复1HP！杀敌传播！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            BuffedPlayers.Clear();
            _lastHealTime = 0;
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;
            if (victim == null || !victim.IsValid || attacker == victim) return HookResult.Continue;
            if (victim.TeamNum == attacker.TeamNum) return HookResult.Continue;

            // Pick a random alive enemy who doesn't already have the buff
            var candidates = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum != attacker.TeamNum
                    && p != attacker
                    && !BuffedPlayers.Contains(p.SteamID)
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            if (candidates.Count == 0) return HookResult.Continue;

            CCSPlayerController target = candidates[_random.Next(candidates.Count)];
            BuffedPlayers.Add(target.SteamID);

            CCSPlayerPawn targetPawn = target.PlayerPawn!.Value!;
            targetPawn.VelocityModifier = _config.Dices.Karma.SpeedMultiplier;
            Utilities.SetStateChanged(targetPawn, "CCSPlayerPawn", "m_flVelocityModifier");

            target.PrintToCenterAlert("☸ 因果报应！移速×1.5 + 每秒回复1HP！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Karma_spread"].Value.Replace("{attacker}", attacker.PlayerName).Replace("{target}", target.PlayerName)}");

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (BuffedPlayers.Count == 0) return;

            float now = (float)Server.CurrentTime;

            // HP regen: every 1 second
            if (now - _lastHealTime >= 1f)
            {
                _lastHealTime = now;
                int hp = _config.Dices.Karma.HpPerSecond;

                foreach (var p in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && BuffedPlayers.Contains(p.SteamID)
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                    if (pawn.Health < pawn.MaxHealth)
                    {
                        pawn.Health = Math.Min(pawn.Health + hp, pawn.MaxHealth);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                }

                // Clean dead from buffed set
                BuffedPlayers.RemoveWhere(steamId =>
                {
                    var player = Utilities.GetPlayers().FirstOrDefault(pl => pl.SteamID == steamId);
                    return player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE;
                });
            }

            // Maintain speed buff
            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && BuffedPlayers.Contains(p.SteamID)
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                if (pawn.VelocityModifier != _config.Dices.Karma.SpeedMultiplier)
                {
                    pawn.VelocityModifier = _config.Dices.Karma.SpeedMultiplier;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
