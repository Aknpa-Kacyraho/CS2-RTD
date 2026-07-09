using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Martyrdom : DiceBlueprint
    {
        public override string ClassName => "Martyrdom";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerDeath"
        ];

        public Martyrdom(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "GrenadeKing");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "爆炸艺术家", "殉道爆炸范围翻倍");
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null
                || !victim.IsValid
                || !_players.Contains(victim)
                || victim.PlayerPawn?.Value == null
                || !victim.PlayerPawn.Value.IsValid
                || victim.PlayerPawn.Value.AbsOrigin == null)
            {
                return HookResult.Continue;
            }

            Vector deathPos = victim.PlayerPawn.Value.AbsOrigin!;
            float radius = _comboActive ? _config.Dices.Martyrdom.ExplosionRadius * 2f : _config.Dices.Martyrdom.ExplosionRadius;
            int damage = _config.Dices.Martyrdom.ExplosionDamage;

            // Delayed explosion for dramatic effect
            new CounterStrikeSharp.API.Modules.Timers.Timer(0.5f, () =>
            {
                // Visual explosion
                CBaseEntity? boom = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
                if (boom != null)
                {
                    boom.Teleport(deathPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    boom.DispatchSpawn();
                    boom.AcceptInput("Explode");
                }

                // Damage nearby enemies
                foreach (CCSPlayerController nearby in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsHLTV
                        && p.PlayerPawn?.Value != null
                        && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                        && p.PlayerPawn.Value.AbsOrigin != null))
                {
                    float dist = Vectors.GetDistance(deathPos, nearby.PlayerPawn.Value.AbsOrigin!);
                    if (dist <= radius)
                    {
                        float falloff = 1.0f - (dist / radius);
                        int dmg = (int)float.Round(damage * falloff);
                        nearby.PlayerPawn.Value.Health -= dmg;
                        Utilities.SetStateChanged(nearby.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                        nearby.PrintToCenterAlert($"💥 亡语爆炸 -{dmg} HP!");
                        if (nearby.PlayerPawn.Value.Health <= 0)
                        {
                            try { nearby.PlayerPawn.Value.CommitSuicide(false, true); }
                            catch
                            {
                                nearby.PlayerPawn.Value.Health = 0;
                                Utilities.SetStateChanged(nearby.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                            }
                        }
                    }
                }
            });

            victim.PrintToCenterAlert("💥 亡语爆炸！");
            return HookResult.Continue;
        }
    }
}
