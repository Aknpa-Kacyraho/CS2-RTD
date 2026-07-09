using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Phoenix : DiceBlueprint
    {
        public override string ClassName => "Phoenix";
        public override bool CanBeDrawn => false;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre", "OnTick"];

        private readonly Dictionary<CCSPlayerController, float> _phoenixEndTime = [];
        private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _phoenixGlows = [];
        private readonly Dictionary<CCSPlayerController, bool> _phoenixExploded = [];
        private readonly Dictionary<CCSPlayerController, float> _cooldownEnd = [];

        public Phoenix(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _phoenixExploded[player] = false;
            _cooldownEnd[player] = 0f;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DeactivatePhoenix(player);
            _ = _players.Remove(player);
            _ = _phoenixEndTime.Remove(player);
            _ = _phoenixGlows.Remove(player);
            _ = _phoenixExploded.Remove(player);
            _ = _cooldownEnd.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) DeactivatePhoenix(p);
            _players.Clear();
            _phoenixEndTime.Clear();
            _phoenixGlows.Clear();
            _phoenixExploded.Clear();
            _cooldownEnd.Clear();
        }

        public override void Destroy() => Reset();

        private void DeactivatePhoenix(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            if (player.PlayerPawn?.Value is not CCSPlayerPawn pawn || !pawn.IsValid) return;
            if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_WALK);
            pawn.ActualGravityScale = 1.0f;

            if (_phoenixGlows.TryGetValue(player, out var glow))
            {
                GlowUtil.RemoveGlow(glow.Item1, glow.Item2);
                _phoenixGlows.Remove(player);
            }
        }

        public void TriggerPhoenixRevive(CCSPlayerController player)
        {
            if (player?.PlayerPawn?.Value is not CCSPlayerPawn pawn || !pawn.IsValid) return;
            float now = (float)Server.CurrentTime;

            float duration = _config.Dices.Phoenix.InvulDuration;
            _phoenixEndTime[player] = now + duration;
            _cooldownEnd[player] = now + 60f;

            pawn.MoveType = MoveType_t.MOVETYPE_NONE;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NONE);

            pawn.ActualGravityScale = 0.1f;

            _phoenixGlows[player] = GlowUtil.CreateGlow(pawn, Color.Gold);

            player.PrintToCenterAlert($"🔥 菲尼克斯涅槃！{duration}s无敌！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🔥 {player.PlayerName} 触发菲尼克斯！涅槃重生！");
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !_players.Contains(victim))
                return HookResult.Continue;

            float now = (float)Server.CurrentTime;

            if (_phoenixEndTime.TryGetValue(victim, out float endTime) && now < endTime)
            {
                info.Damage = 0;
                return HookResult.Changed;
            }

            if (_cooldownEnd.TryGetValue(victim, out float cd) && now < cd)
                return HookResult.Continue;

            int healthAfterDamage = entity.Health - (int)float.Round(info.Damage);
            if (healthAfterDamage > 0) return HookResult.Continue;

            info.Damage = 0;
            _phoenixExploded[victim] = false;

            CCSPlayerPawn pawn = entity.As<CCSPlayerPawn>();
            if (pawn != null)
            {
                pawn.Health = Math.Max(pawn.Health, 1);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }

            CCSPlayerController capturedVictim = victim;
            Server.NextFrame(() =>
            {
                TriggerPhoenixRevive(capturedVictim);
            });

            return HookResult.Changed;
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            foreach (var kv in _phoenixEndTime.ToList())
            {
                var player = kv.Key;
                float endTime = kv.Value;

                if (now >= endTime)
                {
                    DeactivatePhoenix(player);
                    _ = _phoenixEndTime.Remove(player);

                    if (_phoenixExploded.TryGetValue(player, out bool exploded) && !exploded)
                    {
                        _phoenixExploded[player] = true;

                        if (player?.PlayerPawn?.Value is CCSPlayerPawn pawn && pawn.IsValid
                            && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                        {
                            pawn.Health = _config.Dices.Phoenix.HealHP;
                            pawn.MaxHealth = _config.Dices.Phoenix.HealHP;
                            pawn.ArmorValue = _config.Dices.Phoenix.HealArmor;
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                            player.GiveNamedItem("weapon_ak47");

                            Vector? deathPos = pawn.AbsOrigin;
                            if (deathPos != null)
                            {
                                float radius = _config.Dices.Phoenix.ExplosionRadius;
                                int damage = _config.Dices.Phoenix.ExplosionDamage;
                                Vector pos = deathPos;

                                CBaseEntity? boom = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
                                if (boom != null)
                                {
                                    boom.Teleport(pos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                                    boom.DispatchSpawn();
                                    boom.AcceptInput("Explode");
                                }

                                foreach (var nearby in Utilities.GetPlayers()
                                    .Where(p => p.IsValid && !p.IsHLTV && p != player
                                        && p.Pawn?.Value != null && p.Pawn.Value.IsValid
                                        && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                                        && p.Pawn.Value.AbsOrigin != null))
                                {
                                    float dist = Vectors.GetDistance(pos, nearby.Pawn.Value.AbsOrigin!);
                                    if (dist <= radius)
                                    {
                                        float falloff = 1.0f - (dist / radius);
                                        int dmg = (int)float.Round(damage * falloff);
                                        nearby.Pawn.Value.Health -= dmg;
                                        Utilities.SetStateChanged(nearby.Pawn.Value, "CBaseEntity", "m_iHealth");
                                        if (nearby.Pawn.Value.Health <= 0)
                                        {
                                            if (!nearby.IsBot && !nearby.IsHLTV)
                                                nearby.Pawn.Value.CommitSuicide(false, true);
                                            else
                                            {
                                                try { nearby.Pawn.Value.CommitSuicide(false, true); }
                                                catch
                                                {
                                                    nearby.Pawn.Value.Health = 0;
                                                    Utilities.SetStateChanged(nearby.Pawn.Value, "CBaseEntity", "m_iHealth");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            player.PrintToCenterAlert("🔥 菲尼克斯涅槃完成！444HP/444甲/AK！");
                            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🔥 {player.PlayerName} 菲尼克斯涅槃重生！");
                        }
                    }
                    continue;
                }

                if (player?.PlayerPawn?.Value is CCSPlayerPawn p && p.IsValid)
                {
                    if (p.MoveType != MoveType_t.MOVETYPE_NONE)
                    {
                        p.MoveType = MoveType_t.MOVETYPE_NONE;
                        Schema.SetSchemaValue(p.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NONE);
                    }

                    if (p.ActualGravityScale > 0.15f)
                        p.ActualGravityScale = 0.1f;

                    if (p.AbsOrigin != null)
                    {
                        float floatSpeed = _config.Dices.Phoenix.FloatSpeed * Server.TickInterval;
                        Vector newPos = new(p.AbsOrigin.X, p.AbsOrigin.Y, p.AbsOrigin.Z + floatSpeed);
                        p.Teleport(newPos, p.AbsRotation, new Vector(0, 0, floatSpeed / Server.TickInterval));
                    }
                }
            }
        }
    }
}
