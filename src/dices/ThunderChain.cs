using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class ThunderChain : DiceBlueprint
    {
        public override string ClassName => "ThunderChain";
        private bool _comboActive;
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public ThunderChain(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "LaserCage");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "雷光炼狱", "雷霆+2次弹射");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset() => _players.Clear();
        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;

            if (attacker == null || !attacker.IsValid || attacker.IsHLTV || !_players.Contains(attacker)
                || victim == null || !victim.IsValid
                || attacker == victim
                || victim.PlayerPawn?.Value?.AbsOrigin == null)
                return HookResult.Continue;

            Vector corpseOrigin = new(
                victim.PlayerPawn.Value.AbsOrigin.X,
                victim.PlayerPawn.Value.AbsOrigin.Y,
                victim.PlayerPawn.Value.AbsOrigin.Z + 32f);

            float range = _config.Dices.ThunderChain.ChainRange;
            int chainCount = _random.Next(_config.Dices.ThunderChain.ChainMin,
                _config.Dices.ThunderChain.ChainMax + (_comboActive ? 3 : 1));

            List<CCSPlayerController> enemies = [];
            foreach (CCSPlayerController p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p != attacker
                    && p.TeamNum != attacker.TeamNum
                    && p.PlayerPawn?.Value != null
                    && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.PlayerPawn.Value.AbsOrigin != null))
            {
                float dist = Vectors.GetDistance(corpseOrigin, p.PlayerPawn!.Value!.AbsOrigin!);
                if (dist <= range * 2f)
                    enemies.Add(p);
            }

            if (enemies.Count == 0) return HookResult.Continue;

            HashSet<CCSPlayerController> hit = [];
            List<(CCSPlayerController Target, int Damage, Vector HitPos)> chain = [];
            Vector currentOrigin = corpseOrigin;
            int baseDamage = _random.Next(_config.Dices.ThunderChain.InitialDamageMin,
                _config.Dices.ThunderChain.InitialDamageMax + 1);
            float decay = _config.Dices.ThunderChain.DamageDecay;

            for (int hop = 0; hop < chainCount; hop++)
            {
                CCSPlayerController? nearest = null;
                float nearestDist = range;

                foreach (var enemy in enemies)
                {
                    if (hit.Contains(enemy)) continue;
                    if (enemy.PlayerPawn?.Value?.AbsOrigin == null) continue;
                    float d = Vectors.GetDistance(currentOrigin, enemy.PlayerPawn.Value.AbsOrigin);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearest = enemy;
                    }
                }

                if (nearest == null) break;

                hit.Add(nearest);
                int dmg = Math.Max(10, (int)(baseDamage * MathF.Pow(1f - decay, hop)));
                Vector hitPos = new(
                    nearest.PlayerPawn!.Value!.AbsOrigin!.X,
                    nearest.PlayerPawn.Value.AbsOrigin.Y,
                    nearest.PlayerPawn.Value.AbsOrigin.Z + 32f);

                chain.Add((nearest, dmg, hitPos));
                currentOrigin = hitPos;
            }

            if (chain.Count == 0) return HookResult.Continue;

            Vector prevPos = corpseOrigin;
            int totalChain = chain.Count;

            for (int i = 0; i < totalChain; i++)
            {
                float delay = i * 0.3f;
                var (target, dmg, hitPos) = chain[i];
                Vector fromPos = prevPos;
                prevPos = hitPos;

                Action applyDamage = () =>
                {
                    if (target == null || !target.IsValid
                        || target.PlayerPawn?.Value == null || !target.PlayerPawn.Value.IsValid
                        || target.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        return;

                    CCSPlayerPawn targetPawn = target.PlayerPawn.Value;
                    targetPawn.Health = Math.Max(0, targetPawn.Health - dmg);
                    Utilities.SetStateChanged(targetPawn, "CBaseEntity", "m_iHealth");

                    target.PrintToCenterAlert($"⚡ 雷劫 -{dmg} HP!");
                    if (targetPawn.Health <= 0)
                    {
                        if (!target.IsBot && !target.IsHLTV)
                            targetPawn.CommitSuicide(false, true);
                        else
                        {
                            try { targetPawn.CommitSuicide(false, true); }
                            catch
                            {
                                targetPawn.Health = 0;
                                Utilities.SetStateChanged(targetPawn, "CBaseEntity", "m_iHealth");
                            }
                        }
                    }
                };

                if (delay <= 0f)
                {
                    Server.NextFrame(() =>
                    {
                        applyDamage();
                        try { SpawnLightningVisuals(fromPos, hitPos); } catch { }
                    });
                }
                else
                {
                    new CounterStrikeSharp.API.Modules.Timers.Timer(delay, () =>
                    {
                        applyDamage();
                        try { SpawnLightningVisuals(fromPos, hitPos); } catch { }
                    });
                }
            }

            attacker.PrintToCenterAlert($"⚡ 雷劫连锁 x{chain.Count}!");

            return HookResult.Continue;
        }

        private static void SpawnLightningVisuals(Vector fromPos, Vector hitPos)
        {
            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam != null && beam.IsValid)
            {
                beam.Render = Color.FromArgb(255, 100, 200, 255);
                beam.Width = 3f;
                beam.Teleport(fromPos, new QAngle(), new Vector());
                beam.EndPos.X = hitPos.X;
                beam.EndPos.Y = hitPos.Y;
                beam.EndPos.Z = hitPos.Z;
                beam.DispatchSpawn();
                new CounterStrikeSharp.API.Modules.Timers.Timer(0.5f, () =>
                {
                    if (beam != null && beam.IsValid) beam.Remove();
                });
            }

            CBaseEntity? boom = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
            if (boom != null && boom.IsValid)
            {
                boom.Teleport(hitPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                boom.DispatchSpawn();
                boom.AcceptInput("Explode");
                new CounterStrikeSharp.API.Modules.Timers.Timer(0.5f, () =>
                {
                    if (boom != null && boom.IsValid)
                        boom.AcceptInput("Kill");
                });
            }
        }
    }
}
