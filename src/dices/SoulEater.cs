using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class SoulEater : DiceBlueprint
    {
        public override string ClassName => "SoulEater";
        public override List<string> Events => ["EventPlayerDeath"];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private bool _comboActive;
        private static readonly string[] ThrowablePool =
        [
            "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade",
            "weapon_molotov", "weapon_incgrenade", "weapon_decoy"
        ];

        public SoulEater(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Vampire");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;

            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)
                || victim == null || !victim.IsValid || attacker == victim)
                return HookResult.Continue;

            if (attacker.PlayerPawn?.Value == null || !attacker.PlayerPawn.Value.IsValid) return HookResult.Continue;
            if (victim.PlayerPawn?.Value?.AbsOrigin == null) return HookResult.Continue;

            // Soul drain particle from corpse to attacker
            Vector corpsePos = victim.PlayerPawn.Value.AbsOrigin;
            CParticleSystem? particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle != null)
            {
                particle.EffectName = "particles/ui/ui_experience_award_innerpoint.vpcf";
                particle.Teleport(corpsePos, new QAngle(), new Vector());
                particle.StartActive = true;
                particle.DispatchSpawn();

                // Attach to attacker so it appears to fly toward them
                if (attacker.PlayerPawn.IsValid && attacker.PlayerPawn.Value.AbsOrigin != null)
                {
                    particle.AcceptInput("SetParent", attacker.PlayerPawn.Value, attacker.PlayerPawn.Value, "!activator");
                }

                new CounterStrikeSharp.API.Modules.Timers.Timer(2f, () =>
                {
                    if (particle != null && particle.IsValid) particle.Remove();
                });
            }

            // Heal the attacker
            int healAmount = _random.Next(_config.Dices.SoulEater.HealMin,
                _config.Dices.SoulEater.HealMax + 1);
            CCSPlayerPawn aPawn = attacker.PlayerPawn.Value;
            int newHealth = Math.Min(aPawn.Health + healAmount, aPawn.MaxHealth);
            int actualHeal = newHealth - aPawn.Health;
            aPawn.Health = newHealth;
            Utilities.SetStateChanged(aPawn, "CBaseEntity", "m_iHealth");

            // Give 2 random throwables
            CCSPlayerController capturedAttacker = attacker;
            Server.NextFrame(() =>
            {
                if (capturedAttacker == null || !capturedAttacker.IsValid) return;
                for (int i = 0; i < 2; i++)
                {
                    string throwable = ThrowablePool[_random.Next(ThrowablePool.Length)];
                    capturedAttacker.GiveNamedItem(throwable);
                }
            });

            // Combo: Vampire + SoulEater — also restore armor
            if (_comboActive)
            {
                aPawn.ArmorValue = Math.Min(aPawn.ArmorValue + 25, 100);
                Utilities.SetStateChanged(aPawn, "CCSPlayerPawn", "m_ArmorValue");
            }

            attacker.PrintToCenterAlert($"👻 噬魂 +{actualHeal} HP! +2 投掷物!" +
                (_comboActive ? " +25甲" : ""));

            return HookResult.Continue;
        }
    }
}
