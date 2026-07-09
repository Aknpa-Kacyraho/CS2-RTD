using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Empress : DiceBlueprint
    {
        public override string ClassName => "Empress";
        private bool _comboActive;
        public override List<string> Listeners => [
            "OnTick"
        ];
        private readonly Dictionary<CCSPlayerController, int> _lastKnownMoney = [];
        private readonly Dictionary<CCSPlayerController, int> _totalEarned = [];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public Empress(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Emperor");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "王权永恒", "王权永恒联动生效！");

            if (player.InGameMoneyServices != null)
            {
                _lastKnownMoney[player] = player.InGameMoneyServices.Account;
                _totalEarned[player] = 0;
            }

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _lastKnownMoney.Remove(player);
            _ = _totalEarned.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _lastKnownMoney.Clear();
            _totalEarned.Clear();
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.InGameMoneyServices == null)
                        continue;

                    if (!_lastKnownMoney.TryGetValue(player, out int lastMoney))
                    {
                        _lastKnownMoney[player] = player.InGameMoneyServices.Account;
                        continue;
                    }

                    int currentMoney = player.InGameMoneyServices.Account;
                    if (currentMoney > lastMoney)
                    {
                        int increase = currentMoney - lastMoney;
                        // Double the increase
                        player.InGameMoneyServices.Account += increase;
                        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

                        // Track total earned this round (doubled, to match actual income)
                        if (!_totalEarned.ContainsKey(player))
                            _totalEarned[player] = 0;
                        _totalEarned[player] += increase * 2;

                        // Emperor combo: revive threshold halved (1200 → 600)
                        int threshold = _comboActive ? 600 : 1200;
                        if (_totalEarned[player] >= threshold)
                        {
                            _totalEarned[player] -= threshold;
                            TryReviveTeammate(player);
                        }
                    }

                    _lastKnownMoney[player] = player.InGameMoneyServices.Account;
                }
                catch { }
            }
        }

        private void TryReviveTeammate(CCSPlayerController empress)
        {
            // Find a random dead teammate
            var deadTeammates = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p != empress
                    && p.TeamNum == empress.TeamNum
                    && p.PlayerPawn?.Value != null
                    && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                .OrderBy(_ => _random.Next())
                .ToList();

            if (deadTeammates.Count == 0) return;

            var deadTeammate = deadTeammates.First();
            CCSPlayerController capturedDead = deadTeammate;
            CCSPlayerController capturedEmpress = empress;

            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    if (capturedDead?.PlayerPawn?.Value == null
                        || capturedDead.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                        return;

                    capturedDead.Respawn();

                    Server.NextFrame(() =>
                    {
                        if (capturedDead?.PlayerPawn?.Value == null
                            || capturedDead.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                            return;

                        capturedDead.RemoveWeapons();
                        capturedDead.GiveNamedItem("weapon_knife");

                        if (capturedDead.Team == CsTeam.CounterTerrorist)
                        {
                            CCSPlayer_ItemServices itemServices = new(capturedDead.PlayerPawn.Value.ItemServices!.Handle)
                            {
                                HasDefuser = true
                            };
                        }

                        // Give default loadout
                        capturedDead.GiveNamedItem("weapon_ak47");
                        capturedDead.GiveNamedItem("weapon_deagle");
                        capturedDead.PlayerPawn.Value.ArmorValue = 100;

                        capturedDead.PrintToCenterAlert("👑 女皇复活了你!");
                        capturedEmpress?.PrintToCenterAlert($"👑 你复活了 {capturedDead.PlayerName}!");

                        string prefix = _localizer["command.prefix"].Value;
                        Server.PrintToChatAll($" {prefix}{_localizer["dice_Empress_revive"].Value.Replace("{empressName}", capturedEmpress.PlayerName).Replace("{deadName}", capturedDead.PlayerName)}");
                    });
                });
            });
        }
    }
}
