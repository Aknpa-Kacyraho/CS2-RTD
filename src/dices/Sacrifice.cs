using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class Sacrifice : DiceBlueprint
    {
        public override string ClassName => "Sacrifice";
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnTick"];

        // Track teammate death count per dice holder
        private readonly Dictionary<CCSPlayerController, int> _teammateDeathCount = [];
        // Track revived teammates' speed bonus end time
        private readonly Dictionary<CCSPlayerController, float> _speedBonusEndTime = [];

        public Sacrifice(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _teammateDeathCount[player] = 0;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _teammateDeathCount.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _teammateDeathCount.Clear();
            _speedBonusEndTime.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;

            foreach (var holder in _players.ToList())
            {
                if (holder == null || !holder.IsValid) continue;
                // Only count teammate deaths (same team, not self)
                if (victim == null || !victim.IsValid || victim == holder) continue;
                if (victim.TeamNum != holder.TeamNum) continue;

                int current = _teammateDeathCount.TryGetValue(holder, out int c) ? c + 1 : 1;
                _teammateDeathCount[holder] = current;

                int required = _config.Dices.Sacrifice.RequiredTeammateDeaths;

                // Show progress to the holder
                holder.PrintToChat(_localizer["command.prefix"].Value +
                    _localizer["dice_Sacrifice_progress"].Value.Replace("{current}", current.ToString()).Replace("{required}", required.ToString()));

                // Check if holder is alive
                if (holder.PlayerPawn?.Value == null || !holder.PlayerPawn.Value.IsValid
                    || holder.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                if (current < required) continue;

                // TRIGGER: revive random dead teammate, then sacrifice self
                _teammateDeathCount[holder] = 0; // reset

                string holderName = holder.PlayerName;
                int holderTeam = holder.TeamNum;

                // Find dead teammates
                var deadTeammates = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.TeamNum == holderTeam
                        && p != holder
                        && (p.PlayerPawn?.Value == null || !p.PlayerPawn.Value.IsValid
                            || p.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE))
                    .ToList();

                if (deadTeammates.Count == 0)
                {
                    holder.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_Sacrifice_no_target"].Value}");
                    continue;
                }

                CCSPlayerController reviveTarget = deadTeammates[Random.Shared.Next(deadTeammates.Count)];
                string revivedName = reviveTarget.PlayerName;

                // Revive the teammate (pattern from Emperor.cs)
                Server.NextFrame(() =>
                {
                    Server.NextFrame(() =>
                    {
                        if (reviveTarget?.PlayerPawn?.Value != null
                            && reviveTarget.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                            return; // already revived somehow

                        reviveTarget.Respawn();

                        Server.NextFrame(() =>
                        {
                            if (reviveTarget?.PlayerPawn?.Value == null
                                || reviveTarget.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                                return;

                            CCSPlayerPawn targetPawn = reviveTarget.PlayerPawn.Value;

                            // Remove weapons, give knife + AK47
                            reviveTarget.RemoveWeapons();
                            reviveTarget.GiveNamedItem("weapon_knife");
                            reviveTarget.GiveNamedItem("weapon_ak47");

                            // Full armor + 300 HP
                            targetPawn.ArmorValue = _config.Dices.Sacrifice.ReviveArmor;
                            Utilities.SetStateChanged(targetPawn, "CCSPlayerPawn", "m_ArmorValue");

                            targetPawn.Health = _config.Dices.Sacrifice.ReviveHP;
                            targetPawn.MaxHealth = _config.Dices.Sacrifice.ReviveHP;
                            Utilities.SetStateChanged(targetPawn, "CBaseEntity", "m_iHealth");
                            Utilities.SetStateChanged(targetPawn, "CBaseEntity", "m_iMaxHealth");

                            // 2x speed
                            targetPawn.VelocityModifier = _config.Dices.Sacrifice.SpeedMultiplier;
                            Utilities.SetStateChanged(targetPawn, "CCSPlayerPawn", "m_flVelocityModifier");
                            _speedBonusEndTime[reviveTarget] = float.MaxValue; // lasts until round end

                            // CT gets defuser
                            if (reviveTarget.Team == CsTeam.CounterTerrorist)
                            {
                                var itemServices = new CCSPlayer_ItemServices(targetPawn.ItemServices!.Handle)
                                {
                                    HasDefuser = true
                                };
                            }

                            reviveTarget.PrintToCenterAlert("💀 你被献祭复活了！300HP 300甲 AK47 速度×2");
                        });
                    });
                });

                // Sacrifice self after a short delay (so revive happens first)
                new CounterStrikeSharp.API.Modules.Timers.Timer(0.5f, () =>
                {
                    if (holder != null && holder.IsValid
                        && holder.PlayerPawn?.Value != null && holder.PlayerPawn.Value.IsValid
                        && holder.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        if (!holder.IsBot && !holder.IsHLTV)
                            holder.PlayerPawn.Value.CommitSuicide(false, true);
                    }
                });

                // Full server broadcast
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Sacrifice_broadcast"].Value.Replace("{holder}", holderName).Replace("{revived}", revivedName)}");

                break; // only trigger once per death event per holder
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            // Maintain speed bonus for revived players
            if (_speedBonusEndTime.Count == 0) return;

            foreach (var kv in _speedBonusEndTime.ToList())
            {
                var player = kv.Key;
                if (player?.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    _ = _speedBonusEndTime.Remove(player);
                    continue;
                }

                // Maintain VelocityModifier (engine resets it)
                if (player.PlayerPawn.Value.VelocityModifier != _config.Dices.Sacrifice.SpeedMultiplier)
                {
                    player.PlayerPawn.Value.VelocityModifier = _config.Dices.Sacrifice.SpeedMultiplier;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
