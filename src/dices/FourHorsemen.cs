using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Linq;

namespace RollTheDice.Dices
{
    public class FourHorsemen : DiceBlueprint
    {
        public override string ClassName => "FourHorsemen";
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private static readonly Dictionary<ulong, string> _assignments = []; // steamID → "war"/"plague"/"famine"/"death"
        private static readonly HashSet<ulong> _plagueInfected = [];
        private static float _lastPlagueTick;
        private static bool _active;
        private static readonly string[] _horsemenList = ["war", "plague", "famine", "death"];
        private static readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public FourHorsemen(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
            : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            if (!_active)
            {
                _active = true;
                AssignHorsemen();
                AnnounceAll();
            }

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            // Clean up War damage bonuses
            foreach (var kvp in _assignments)
            {
                if (kvp.Value == "war")
                    DamageBonusManager.UnregisterBySteamId(kvp.Key, "FourHorsemenWar");
            }
            _players.Clear();
            _assignments.Clear();
            _plagueInfected.Clear();
            _active = false;
            _lastPlagueTick = 0;
        }

        public override void Destroy() => Reset();

        private void AssignHorsemen()
        {
            _assignments.Clear();
            _plagueInfected.Clear();

            var alivePlayers = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .ToList();

            if (alivePlayers.Count == 0) return;

            foreach (var p in alivePlayers)
            {
                string horseman = _horsemenList[_random.Next(_horsemenList.Length)];
                _assignments[p.SteamID] = horseman;

                if (horseman == "war")
                    DamageBonusManager.RegisterBySteamId(p.SteamID, "FourHorsemenWar", _config.Dices.FourHorsemen.WarDamageBonus);
                else if (horseman == "plague")
                    _plagueInfected.Add(p.SteamID);
            }

            _lastPlagueTick = (float)Server.CurrentTime;
        }

        private void AnnounceAll()
        {
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV))
            {
                if (_assignments.TryGetValue(p.SteamID, out string? horseman))
                {
                    string name = horseman switch
                    {
                        "war" => _localizer["dice_FourHorsemen_war_name"].Value,
                        "plague" => _localizer["dice_FourHorsemen_plague_name"].Value,
                        "famine" => _localizer["dice_FourHorsemen_famine_name"].Value,
                        "death" => _localizer["dice_FourHorsemen_death_name"].Value,
                        _ => "???"
                    };
                    p.PrintToCenterAlert($"⚖️ 四骑士降临！你是：{name}");
                    p.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_FourHorsemen_assigned"].Value.Replace("{horseman}", name)}");
                }
            }

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_FourHorsemen_broadcast"].Value}");
        }

        // War: +50% damage dealt; Plague: attack infects; Famine: attacker clip cleared
        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (!_active) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();

            if (victim == null || !victim.IsValid) return HookResult.Continue;

            // War (战争): victim takes +50% extra damage
            if (_assignments.TryGetValue(victim.SteamID, out string? vType) && vType == "war")
            {
                info.Damage *= (1f + _config.Dices.FourHorsemen.WarDamageTaken);
            }

            if (attacker == null || !attacker.IsValid || attacker == victim) return HookResult.Continue;

            // War (战争): attacker deals +50% bonus damage
            if (DamageBonusManager.HasAny(attacker) && DamageBonusManager.IsHighest(attacker, "FourHorsemenWar"))
            {
                float effective = DamageBonusManager.GetEffective(attacker);
                info.Damage *= (1f + effective);
            }

            // Plague (瘟疫): plague holder's attack infects the victim
            if (_assignments.TryGetValue(attacker.SteamID, out string? aType) && aType == "plague")
            {
                if (!_plagueInfected.Contains(victim.SteamID))
                {
                    _plagueInfected.Add(victim.SteamID);
                    victim.PrintToCenterAlert($"🦠 你被{_localizer["dice_FourHorsemen_plague_name"].Value}传染了！");
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_FourHorsemen_plague_spread"].Value.Replace("{attacker}", attacker.PlayerName).Replace("{victim}", victim.PlayerName)}");
                }
            }

            // Famine (饥荒): hitting famine holder clears attacker's clip
            if (_assignments.TryGetValue(victim.SteamID, out string? vType2) && vType2 == "famine")
            {
                if (attacker.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value != null)
                {
                    var activeWeapon = attacker.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value;
                    string? weaponName = activeWeapon.DesignerName;
                    if (weaponName != null && !weaponName.Contains("knife") && !weaponName.Contains("c4") && !weaponName.Contains("taser"))
                    {
                        activeWeapon.Clip1 = 0;
                        attacker.PrintToCenterAlert($"🍞 饥荒！弹夹清零！");
                    }
                }
            }

            return HookResult.Continue;
        }

        // Death (死亡): killer takes 100 damage when death holder dies
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (!_active) return HookResult.Continue;

            CCSPlayerController? deadPlayer = @event.Userid;
            if (deadPlayer == null || !deadPlayer.IsValid) return HookResult.Continue;

            bool isDeath = _assignments.TryGetValue(deadPlayer.SteamID, out string? type) && type == "death";
            CCSPlayerController? killer = isDeath ? @event.Attacker : null;
            ulong killerSid = (killer != null && killer.IsValid && !killer.IsHLTV) ? killer.SteamID : 0;
            ulong deadSid = deadPlayer.SteamID;

            // Defer all state changes to NextFrame (avoid modifying players during death event)
            Server.NextFrame(() =>
            {
                if (isDeath && killerSid != 0)
                {
                    var k = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == killerSid);
                    if (k?.PlayerPawn?.Value != null && k.PlayerPawn.Value.IsValid
                        && k.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        if (!k.IsBot && !k.IsHLTV)
                            k.PlayerPawn.Value.CommitSuicide(false, true);
                        else
                        {
                            try { k.PlayerPawn.Value.CommitSuicide(false, true); }
                            catch { k.PlayerPawn.Value.Health = 0; Utilities.SetStateChanged(k.PlayerPawn.Value, "CBaseEntity", "m_iHealth"); }
                        }
                        k.PrintToCenterAlert("☠️ 死亡骑士的反噬！");
                    }
                }

                _assignments.Remove(deadSid);
                _plagueInfected.Remove(deadSid);
                DamageBonusManager.UnregisterBySteamId(deadSid, "FourHorsemenWar");
            });

            return HookResult.Continue;
        }

        // Plague: -HP per second for all infected
        public void OnTick()
        {
            if (!_active) return;

            float now = (float)Server.CurrentTime;
            if (now - _lastPlagueTick < 1f) return;
            _lastPlagueTick = now;

            int dmg = _config.Dices.FourHorsemen.PlagueDamagePerSecond;

            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && _plagueInfected.Contains(p.SteamID)
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                pawn.Health -= dmg;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                if (pawn.Health <= 0)
                {
                    _plagueInfected.Remove(p.SteamID);
                    _assignments.Remove(p.SteamID);
                    if (!p.IsBot)
                        pawn.CommitSuicide(false, true);
                }
            }

            // Clean up dead/disconnected from infected set
            _plagueInfected.RemoveWhere(steamId =>
            {
                var player = Utilities.GetPlayers().FirstOrDefault(pl => pl.SteamID == steamId);
                return player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE;
            });
            var deadAssignKeys = _assignments
                .Where(kvp =>
                {
                    var player = Utilities.GetPlayers().FirstOrDefault(pl => pl.SteamID == kvp.Key);
                    return player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE;
                })
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in deadAssignKeys)
                _assignments.Remove(key);
        }
    }
}
