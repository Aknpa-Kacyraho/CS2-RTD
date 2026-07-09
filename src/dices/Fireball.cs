using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Fireball : DiceBlueprint
    {
        public override string ClassName => "Fireball";
        public override List<string> Events => [
            "EventMolotovDetonate"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private bool _comboActive;

        public Fireball(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.Pawn?.Value == null || !player.Pawn.Value.IsValid) return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "FireLord");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "焚天灭地", "火球术+炎魔！爆炸范围50%↑，伤害50%↑");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public HookResult EventMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;

            // Only Fireball holder's molotovs get enhanced
            CCSPlayerController? thrower = @event.Userid;
            if (thrower == null || !thrower.IsValid || !_players.Contains(thrower))
                return HookResult.Continue;

            Vector origin = new(@event.X, @event.Y, @event.Z);
            float comboBoost = _comboActive ? 1.5f : 1f;
            float radius = 350f * _config.Dices.Fireball.RadiusMultiplier * comboBoost;
            int damage = (int)float.Round(
                _random.Next(_config.Dices.Fireball.DamageMin, _config.Dices.Fireball.DamageMax + 1) * comboBoost);

            // Create visual explosion
            Server.NextFrame(() =>
            {
                CBaseEntity? boom = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
                if (boom != null)
                {
                    boom.Teleport(origin, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    boom.DispatchSpawn();
                    boom.AcceptInput("Explode");
                }
            });

            // Damage enemies in radius
            foreach (CCSPlayerController enemy in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p.Pawn?.Value != null
                    && p.Pawn.Value.IsValid && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.Pawn.Value.AbsOrigin != null))
            {
                float dist = Vectors.GetDistance(origin, enemy.Pawn.Value.AbsOrigin!);
                if (dist <= radius)
                {
                    float falloff = 1f - (dist / radius);
                    int dmg = Math.Max(5, (int)(damage * falloff));
                    enemy.Pawn.Value.Health -= dmg;
                    Utilities.SetStateChanged(enemy.Pawn.Value, "CBaseEntity", "m_iHealth");
                    enemy.PrintToCenterAlert($"🔥 火球术 -{dmg}!");
                    if (enemy.Pawn.Value.Health <= 0)
                    {
                        if (!enemy.IsBot && !enemy.IsHLTV)
                            enemy.Pawn.Value.CommitSuicide(false, true);
                        else
                        {
                            try { enemy.Pawn.Value.CommitSuicide(false, true); }
                            catch { enemy.Pawn.Value.Health = 0; Utilities.SetStateChanged(enemy.Pawn.Value, "CBaseEntity", "m_iHealth"); }
                        }
                    }
                }
            }

            return HookResult.Continue;
        }
    }
}
