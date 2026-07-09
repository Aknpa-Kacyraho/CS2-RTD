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
    public class Fate : DiceBlueprint
    {
        public override string ClassName => "Fate";
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];
        public override List<string> Events => ["EventPlayerDeath", "EventWeaponFire"];

        private static readonly Dictionary<ulong, string> _assignments = [];
        private static readonly Dictionary<ulong, float> _orbitDeathTime = [];
        private static readonly Dictionary<ulong, float> _diceLuckNextRoll = [];
        private static readonly Dictionary<ulong, float> _balanceNextTick = [];
        private static readonly Dictionary<ulong, float> _compassNextReveal = [];
        private static readonly Dictionary<ulong, List<(CDynamicProp?, CDynamicProp?)>> _compassActiveGlows = [];
        private static readonly Dictionary<ulong, float> _webDeathTime = [];
        private static readonly Dictionary<ulong, bool> _webTriggered = [];
        private static readonly Dictionary<ulong, float> _darktideFreezeUntil = [];
        private static readonly Dictionary<ulong, float> _dawnRespawnTime = [];
        private static readonly Dictionary<ulong, bool> _dawnActivated = [];
        private static bool _assignedThisRound;
        private static readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private static readonly string[] _fatePool = ["orbit", "balance", "compass", "web", "darktide", "dawn"];

        public Fate(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });

            // First Fate instance this round: assign fates to ALL alive players
            if (!_assignedThisRound)
            {
                _assignedThisRound = true;
                foreach (var p in Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    AssignFate(p);
                }
            }
        }

        private void AssignFate(CCSPlayerController player)
        {
            ulong sid = player.SteamID;
            string fate = _fatePool[_random.Next(_fatePool.Length)];
            _assignments[sid] = fate;
            CCSPlayerPawn pawn = player.PlayerPawn!.Value!;

            switch (fate)
            {
                case "orbit":
                    _orbitDeathTime[sid] = (float)Server.CurrentTime + _config.Dices.Fate.OrbitDeathTime;
                    SpeedBonusManager.Register(player, "FateOrbit", 0.20f);
                    player.PrintToCenterAlert($"🌠 命轨：无限子弹，移速+20%，{_config.Dices.Fate.OrbitDeathTime:F0}秒后死亡");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}🌠 命运·命轨：{_config.Dices.Fate.OrbitDeathTime:F0}秒后死亡，无限子弹+移速+20%");
                    break;
                case "dice_luck":
                    _diceLuckNextRoll[sid] = (float)Server.CurrentTime + _config.Dices.Fate.DiceLuckInterval;
                    player.PrintToCenterAlert($"🎲 骰运：每{_config.Dices.Fate.DiceLuckInterval:F0}秒更换一次命运，抽中命轨立即死亡");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}🎲 命运·骰运：每{_config.Dices.Fate.DiceLuckInterval:F0}秒命运改写");
                    break;
                case "balance":
                    _balanceNextTick[sid] = (float)Server.CurrentTime + 1f;
                    DamageBonusManager.Register(player, "FateBalance", _config.Dices.Fate.BalanceDamageBonus);
                    player.PrintToCenterAlert("⚖ 天秤：每秒流失1HP，伤害+60%");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}⚖ 命运·天秤：每秒-1HP，伤害+60%");
                    break;
                case "compass":
                    _compassNextReveal[sid] = (float)Server.CurrentTime + _config.Dices.Fate.CompassInterval;
                    player.PrintToCenterAlert($"🧭 罗盘：每{_config.Dices.Fate.CompassInterval:F0}秒透视2名敌人{_config.Dices.Fate.CompassDuration:F0}秒，自身也会暴露发光");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}🧭 命运·罗盘：定时透视敌人，自身也会暴露");
                    break;
                case "web":
                    _webTriggered[sid] = false;
                    player.PrintToCenterAlert($"🕸 织网：致命伤害时与最近敌人换位，无敌{_config.Dices.Fate.WebInvulDuration:F0}秒——之后死亡");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}🕸 命运·织网：致死时换位，无敌{_config.Dices.Fate.WebInvulDuration:F0}秒后死亡");
                    break;
                case "darktide":
                    SpeedBonusManager.Register(player, "FateDarktide", -_config.Dices.Fate.DarktideSlowAmount);
                    player.PrintToCenterAlert($"🌑 暗潮：死亡时冻结所有敌人{_config.Dices.Fate.DarktideFreezeDuration:F0}秒，移速-{(int)(_config.Dices.Fate.DarktideSlowAmount*100)}%");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}🌑 命运·暗潮：死亡时冻结敌人{_config.Dices.Fate.DarktideFreezeDuration:F0}秒");
                    break;
                case "dawn":
                    _dawnActivated[sid] = false;
                    pawn.MaxHealth = Math.Max(1, pawn.MaxHealth - _config.Dices.Fate.DawnHpPenalty);
                    pawn.Health = Math.Min(pawn.Health, pawn.MaxHealth);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    player.PrintToCenterAlert($"🌅 黎明：死亡{_config.Dices.Fate.DawnFreezeDuration:F0}秒后满血满甲复活，开局HP-{_config.Dices.Fate.DawnHpPenalty}");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}🌅 命运·黎明：死后复活，HP-{_config.Dices.Fate.DawnHpPenalty}");
                    break;
            }

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🔮 {player.PlayerName} 的命运——{FateDisplayName(fate)}！");
        }

        private static string FateDisplayName(string fate) => fate switch
        {
            "orbit" => "命轨",
            "dice_luck" => "骰运",
            "balance" => "天秤",
            "compass" => "罗盘",
            "web" => "织网",
            "darktide" => "暗潮",
            "dawn" => "黎明",
            _ => fate
        };

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (player == null || !player.IsValid) return;
            ulong sid = player.SteamID;
            bool isDawnPending = _assignments.TryGetValue(sid, out string? f) && f == "dawn"
                && _dawnRespawnTime.ContainsKey(sid) && !_dawnActivated.GetValueOrDefault(sid);
            if (isDawnPending)
            {
                _orbitDeathTime.Remove(sid);
                _diceLuckNextRoll.Remove(sid);
                _balanceNextTick.Remove(sid);
                _compassNextReveal.Remove(sid);
                ClearCompassGlows(sid);
                _webDeathTime.Remove(sid);
                _webTriggered.Remove(sid);
                _darktideFreezeUntil.Remove(sid);
            }
            else
            {
                CleanupFate(sid);
            }
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                if (p != null && p.IsValid) CleanupFate(p.SteamID);
            _players.Clear();
            _assignments.Clear();
            _orbitDeathTime.Clear();
            _diceLuckNextRoll.Clear();
            _balanceNextTick.Clear();
            _compassNextReveal.Clear();
            foreach (var kv in _compassActiveGlows.ToList()) ClearCompassGlows(kv.Key);
            _webDeathTime.Clear();
            _webTriggered.Clear();
            _darktideFreezeUntil.Clear();
            _dawnRespawnTime.Clear();
            _dawnActivated.Clear();
            _assignedThisRound = false;
        }

        private void CleanupFate(ulong sid)
        {
            var p = Utilities.GetPlayers().FirstOrDefault(x => x.IsValid && x.SteamID == sid);
            if (p != null)
            {
                SpeedBonusManager.Unregister(p, "FateOrbit");
                DamageBonusManager.Unregister(p, "FateBalance");
                SpeedBonusManager.Unregister(p, "FateDarktide");
            }
            _assignments.Remove(sid);
            _orbitDeathTime.Remove(sid);
            _diceLuckNextRoll.Remove(sid);
            _balanceNextTick.Remove(sid);
            _compassNextReveal.Remove(sid);
            ClearCompassGlows(sid);
            _webDeathTime.Remove(sid);
            _webTriggered.Remove(sid);
            _darktideFreezeUntil.Remove(sid);
            _dawnRespawnTime.Remove(sid);
            _dawnActivated.Remove(sid);
        }

        private static void ClearCompassGlows(ulong sid)
        {
            if (_compassActiveGlows.TryGetValue(sid, out var glows))
            {
                foreach (var g in glows) GlowUtil.RemoveGlow(g.Item1, g.Item2);
                _compassActiveGlows.Remove(sid);
            }
        }

        public override void Destroy() => Reset();

        private void RerollFate(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            ulong sid = player.SteamID;
            string oldFate = _assignments.TryGetValue(sid, out var f) ? f : "";

            SpeedBonusManager.Unregister(player, "FateOrbit");
            DamageBonusManager.Unregister(player, "FateBalance");
            SpeedBonusManager.Unregister(player, "FateDarktide");
            _orbitDeathTime.Remove(sid);
            _balanceNextTick.Remove(sid);
            _compassNextReveal.Remove(sid);
            ClearCompassGlows(sid);
            _webTriggered.Remove(sid);
            _webDeathTime.Remove(sid);

            string newFate = _fatePool[_random.Next(_fatePool.Length)];
            _assignments[sid] = newFate;
            CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
            float now = (float)Server.CurrentTime;

            switch (newFate)
            {
                case "orbit":
                    player.PrintToCenterAlert("🎲 骰运翻转——命轨降临！");
                    player.PrintToChat($" {_localizer["command.prefix"].Value}💀 {player.PlayerName} 骰运翻转——命轨！");
                    if (pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        if (!player.IsBot && !player.IsHLTV)
                            pawn.CommitSuicide(false, true);
                        else
                        {
                            try { pawn.CommitSuicide(false, true); }
                            catch { pawn.Health = 0; Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth"); }
                        }
                    }
                    return;
                case "balance":
                    _balanceNextTick[sid] = now + 1f;
                    DamageBonusManager.Register(player, "FateBalance", _config.Dices.Fate.BalanceDamageBonus);
                    player.PrintToCenterAlert("🎲 骰运翻转——天秤！每秒-1HP，伤害+60%");
                    break;
                case "compass":
                    _compassNextReveal[sid] = now + _config.Dices.Fate.CompassInterval;
                    player.PrintToCenterAlert("🎲 骰运翻转——罗盘！透视敌人，自身也会暴露");
                    break;
                case "web":
                    _webTriggered[sid] = false;
                    player.PrintToCenterAlert("🎲 骰运翻转——织网！致死时换位，无敌3秒后死亡");
                    break;
                case "darktide":
                    SpeedBonusManager.Register(player, "FateDarktide", -_config.Dices.Fate.DarktideSlowAmount);
                    player.PrintToCenterAlert("🎲 骰运翻转——暗潮！死亡时冻结所有敌人");
                    break;
                case "dawn":
                    if (_dawnActivated.TryGetValue(sid, out bool used) && used)
                    {
                        player.PrintToCenterAlert("🎲 骰运翻转——黎明！（已用过复活，本次无效）");
                        break;
                    }
                    if (oldFate != "dawn" && pawn != null && pawn.IsValid)
                    {
                        _dawnActivated[sid] = false;
                        pawn.MaxHealth = Math.Max(1, pawn.MaxHealth - _config.Dices.Fate.DawnHpPenalty);
                        pawn.Health = Math.Min(pawn.Health, pawn.MaxHealth);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                    player.PrintToCenterAlert("🎲 骰运翻转——黎明！死后2秒满血满甲复活");
                    break;
            }

            if (newFate == "dice_luck")
                _diceLuckNextRoll[sid] = now + _config.Dices.Fate.DiceLuckInterval;
        }

        public void OnTick()
        {
            float now = (float)Server.CurrentTime;

            // PASS 1: Handle dead-player fates (dawn respawn, etc.) — must run BEFORE the alive check
            foreach (var kv in _assignments.ToList())
            {
                ulong sid = kv.Key;
                string fate = kv.Value;
                if (fate != "dawn") continue;

                var player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && !p.IsHLTV && p.SteamID == sid);
                if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                CCSPlayerPawn pawn = player.PlayerPawn.Value;

                if (_dawnRespawnTime.TryGetValue(sid, out float respawnTime) && now >= respawnTime)
                {
                    _dawnRespawnTime.Remove(sid);
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        player.Respawn();
                        Server.NextFrame(() =>
                        {
                            Server.NextFrame(() =>
                            {
                                if (player?.PlayerPawn?.Value is CCSPlayerPawn p && p.IsValid
                                    && p.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                                {
                                    p.MaxHealth = Math.Max(p.MaxHealth, 100);
                                    p.Health = p.MaxHealth;
                                    p.ArmorValue = 100;
                                    Utilities.SetStateChanged(p, "CBaseEntity", "m_iMaxHealth");
                                    Utilities.SetStateChanged(p, "CBaseEntity", "m_iHealth");
                                    Utilities.SetStateChanged(p, "CCSPlayerPawn", "m_ArmorValue");
                                    p.MoveType = MoveType_t.MOVETYPE_WALK;
                                    Schema.SetSchemaValue(p.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_WALK);
                                    p.Render = Color.FromArgb(255, 255, 255, 255);
                                    Utilities.SetStateChanged(p, "CBaseModelEntity", "m_clrRender");
                                    p.TakesDamage = true;
                                    player.PrintToCenterAlert("🌅 黎明降临！你已重生！");
                                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌅 {player.PlayerName} 的命运·黎明降临！浴火重生！");
                                }
                            });
                        });
                    }
                }
            }

            // PASS 2: Handle alive-player fates
            foreach (var kv in _assignments.ToList())
            {
                ulong sid = kv.Key;
                string fate = kv.Value;
                var player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && !p.IsHLTV && p.SteamID == sid);
                if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                switch (fate)
                {
                    case "orbit":
                        pawn.VelocityModifier = 1.20f;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                        if (_orbitDeathTime.TryGetValue(sid, out float deathTime) && now >= deathTime)
                        {
                            if (!player.IsBot && !player.IsHLTV)
                                pawn.CommitSuicide(false, true);
                            else
                            {
                                try { pawn.CommitSuicide(false, true); }
                                catch { pawn.Health = 0; Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth"); }
                            }
                            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌠 {player.PlayerName} 命轨应验——一切早已注定。");
                        }
                        break;

                    case "dice_luck":
                        if (_diceLuckNextRoll.TryGetValue(sid, out float nextRoll) && now >= nextRoll)
                        {
                            _diceLuckNextRoll[sid] = now + _config.Dices.Fate.DiceLuckInterval;
                            RerollFate(player);
                        }
                        break;

                    case "balance":
                        if (_balanceNextTick.TryGetValue(sid, out float nextTick) && now >= nextTick)
                        {
                            _balanceNextTick[sid] = now + 1f;
                            pawn.Health -= _config.Dices.Fate.BalanceHpLoss;
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
                        break;

                    case "compass":
                        // Clear expired glows
                        if (_compassNextReveal.TryGetValue(sid, out float nextReveal) && now >= nextReveal)
                        {
                            _compassNextReveal[sid] = now + _config.Dices.Fate.CompassInterval;
                            float dur = _config.Dices.Fate.CompassDuration;

                            // Clean up old glows first
                            ClearCompassGlows(sid);

                            // Pick 2 random alive enemies
                            var enemies = Utilities.GetPlayers()
                                .Where(p => p.IsValid && !p.IsHLTV && p != player
                                    && p.TeamNum != player.TeamNum
                                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                                .OrderBy(_ => _random.Next())
                                .Take(2)
                                .ToList();

                            var glowList = new List<(CDynamicProp?, CDynamicProp?)>();
                            foreach (var enemy in enemies)
                                glowList.Add(GlowUtil.CreateGlow(enemy.PlayerPawn!.Value!, Color.Cyan));
                            // Self-glow too
                            glowList.Add(GlowUtil.CreateGlow(pawn, Color.Yellow));
                            _compassActiveGlows[sid] = glowList;

                            // Auto-remove after duration
                            var capSid = sid;
                            _ = new CounterStrikeSharp.API.Modules.Timers.Timer(dur, () => ClearCompassGlows(capSid));

                            player.PrintToCenterAlert($"🧭 罗盘揭示了{enemies.Count}名敌人的位置！{dur:F0}秒");
                        }
                        break;

                    case "darktide":
                        float slow = SpeedBonusManager.GetEffective(player, -_config.Dices.Fate.DarktideSlowAmount);
                        pawn.VelocityModifier = 1 + slow;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                        break;
                }
            }

            // Cleanup expired web death timers
            foreach (var kv in _webDeathTime.ToList())
            {
                if (now >= kv.Value)
                {
                    ulong sid = kv.Key;
                    _webDeathTime.Remove(sid);
                    var p = Utilities.GetPlayers().FirstOrDefault(x => x.IsValid && x.SteamID == sid);
                    if (p?.PlayerPawn?.Value is CCSPlayerPawn wp && wp.IsValid && wp.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        wp.MoveType = MoveType_t.MOVETYPE_WALK;
                        Schema.SetSchemaValue(wp.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_WALK);
                        if (!p.IsBot && !p.IsHLTV)
                            wp.CommitSuicide(false, true);
                        else
                        {
                            try { wp.CommitSuicide(false, true); }
                            catch { wp.Health = 0; Utilities.SetStateChanged(wp, "CBaseEntity", "m_iHealth"); }
                        }
                    }
                }
            }

            // Cleanup expired darktide freezes
            foreach (var kv in _darktideFreezeUntil.ToList())
            {
                if (now >= kv.Value)
                {
                    ulong sid = kv.Key;
                    _darktideFreezeUntil.Remove(sid);
                    var ep = Utilities.GetPlayers().FirstOrDefault(x => x.IsValid && x.SteamID == sid);
                    if (ep?.PlayerPawn?.Value is CCSPlayerPawn epPawn && epPawn.IsValid)
                    {
                        MoveLockManager.Unlock(ep, "FateDarktide");
                        ep.PrintToCenterAlert("🌑 暗潮退去，你恢复了移动！");
                    }
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            bool changed = false;

            if (attacker != null && attacker.IsValid && _assignments.TryGetValue(attacker.SteamID, out string? attFate)
                && attFate == "balance")
            {
                float effective = DamageBonusManager.GetEffective(attacker);
                info.Damage = (int)(info.Damage * (1 + effective));
                changed = true;
            }

            if (victim != null && victim.IsValid && _assignments.TryGetValue(victim.SteamID, out string? vicFate)
                && vicFate == "web" && !_webTriggered.GetValueOrDefault(victim.SteamID))
            {
                int healthAfter = (int)(entity.Health - info.Damage);
                if (healthAfter <= 0)
                {
                    info.Damage = 0;
                    _webTriggered[victim.SteamID] = true;
                    ulong vSid = victim.SteamID;

                    CCSPlayerController? nearest = null;
                    float nearestDist = float.MaxValue;
                    Vector? vOrigin = victim.PlayerPawn?.Value?.AbsOrigin;
                    if (vOrigin == null) return HookResult.Changed;

                    foreach (var ep in Utilities.GetPlayers()
                        .Where(p => p.IsValid && !p.IsHLTV && p != victim
                            && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && p.PlayerPawn.Value.AbsOrigin != null))
                    {
                        float dx = vOrigin.X - ep.PlayerPawn!.Value!.AbsOrigin!.X;
                        float dy = vOrigin.Y - ep.PlayerPawn!.Value!.AbsOrigin!.Y;
                        float d = MathF.Sqrt(dx * dx + dy * dy);
                        if (d < nearestDist) { nearestDist = d; nearest = ep; }
                    }

                    var capV = victim;
                    var capE = nearest;
                    Server.NextFrame(() =>
                    {
                        if (capV?.PlayerPawn?.Value is CCSPlayerPawn vp && vp.IsValid
                            && capE?.PlayerPawn?.Value is CCSPlayerPawn ep && ep.IsValid
                            && vp.AbsOrigin != null && ep.AbsOrigin != null)
                        {
                            Vector vPos = new(vp.AbsOrigin.X, vp.AbsOrigin.Y, vp.AbsOrigin.Z);
                            Vector ePos = new(ep.AbsOrigin.X, ep.AbsOrigin.Y, ep.AbsOrigin.Z);
                            vp.Teleport(ePos, new QAngle(), new Vector());
                            ep.Teleport(vPos, new QAngle(), new Vector());

                            float dur = _config.Dices.Fate.WebInvulDuration;
                            _webDeathTime[vSid] = (float)Server.CurrentTime + dur;
                            MoveLockManager.Lock(capV, "FateWeb");
                            vp.TakesDamage = false;
                            capV.PrintToCenterAlert($"🕸 织网触发！与 {capE.PlayerName} 换位，无敌{dur:F0}s...");
                            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🕸 {capV.PlayerName} 的命运·织网触发！与 {capE.PlayerName} 换位！");
                        }
                    });

                    return HookResult.Changed;
                }
            }

            return changed ? HookResult.Changed : HookResult.Continue;
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid) return HookResult.Continue;
            ulong sid = victim.SteamID;
            if (!_assignments.TryGetValue(sid, out string? fate)) return HookResult.Continue;
            float now = (float)Server.CurrentTime;

            switch (fate)
            {
                case "darktide":
                    foreach (var ep in Utilities.GetPlayers()
                        .Where(p => p.IsValid && !p.IsHLTV
                            && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && p.TeamNum != victim.TeamNum))
                    {
                        float dur = _config.Dices.Fate.DarktideFreezeDuration;
                        _darktideFreezeUntil[ep.SteamID] = now + dur;
                        MoveLockManager.Lock(ep, "FateDarktide");
                        ep.PrintToCenterAlert($"🌑 暗潮降临！无法移动{dur:F0}s！");
                    }
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌑 {victim.PlayerName} 的命运·暗潮！敌人全被定身！");
                    break;

                case "dawn":
                    if (_dawnActivated.TryGetValue(sid, out bool used) && used) return HookResult.Continue;
                    _dawnActivated[sid] = true;
                    float freezeDur = _config.Dices.Fate.DawnFreezeDuration;
                    _dawnRespawnTime[sid] = now + freezeDur;

                    var capV = victim;
                    Server.NextFrame(() =>
                    {
                        if (capV?.PlayerPawn?.Value is not CCSPlayerPawn p || !p.IsValid) return;
                        if (p.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                        {
                            _dawnRespawnTime.Remove(sid);
                            _dawnActivated[sid] = false;
                            return;
                        }
                        p.MoveType = MoveType_t.MOVETYPE_NONE;
                        Schema.SetSchemaValue(p.Handle, "CBaseEntity", "m_nActualMoveType", (int)MoveType_t.MOVETYPE_NONE);
                        p.Render = Color.FromArgb(255, 255, 215, 0);
                        Utilities.SetStateChanged(p, "CBaseModelEntity", "m_clrRender");
                        p.TakesDamage = false;
                        capV.PrintToCenterAlert($"🌅 黎明将至...{freezeDur:F0}s后重生！");
                        Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🌅 {capV.PlayerName} 的命运·黎明触发！{freezeDur:F0}s后重生！");
                    });
                    break;
            }

            return HookResult.Continue;
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid) return HookResult.Continue;
            if (!_assignments.TryGetValue(player.SteamID, out string? fate) || fate != "orbit") return HookResult.Continue;

            if (player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value is CBasePlayerWeapon weapon && weapon.IsValid
                && weapon.VData != null)
            {
                string name = weapon.DesignerName;
                if (!name.Contains("knife") && !name.Contains("bayonet") && !name.Contains("hegrenade")
                    && !name.Contains("flashbang") && !name.Contains("smokegrenade") && !name.Contains("molotov")
                    && !name.Contains("decoy") && !name.Contains("c4") && !name.Contains("taser"))
                {
                    weapon.Clip1 += 1;
                    weapon.ReserveAmmo[0] = Math.Max(weapon.ReserveAmmo[0], 30);
                }
            }

            return HookResult.Continue;
        }
    }
}
