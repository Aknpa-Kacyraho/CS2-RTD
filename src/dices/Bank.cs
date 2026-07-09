using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Bank : DiceBlueprint
    {
        public override string ClassName => "Bank";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick"];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private float _nextPayout;

        public Bank(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Miser");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "资本要塞", "资本要塞联动生效！");
            if (_nextPayout == 0)
                _nextPayout = (float)Server.CurrentTime + _config.Dices.Bank.Interval;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic) { _ = _players.Remove(player); }
        public override void Reset() { _players.Clear(); _nextPayout = 0; }
        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;
            float now = (float)Server.CurrentTime;
            if (now < _nextPayout) return;

            _nextPayout = now + _config.Dices.Bank.Interval;
            int min = _config.Dices.Bank.MinAmount;
            int max = _config.Dices.Bank.MaxAmount;

            foreach (var holder in _players.ToList())
            {
                if (holder == null || !holder.IsValid) continue;
                var teammates = Utilities.GetPlayers()
                    .Where(p => p.IsValid && !p.IsHLTV
                        && p.TeamNum == holder.TeamNum && p != holder
                        && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                        && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    .ToList();
                if (teammates.Count == 0) continue;

                int count = Math.Min(_config.Dices.Bank.TeammatesCount, teammates.Count);
                var picked = new HashSet<CCSPlayerController>();
                for (int i = 0; i < count; i++)
                {
                    CCSPlayerController? target;
                    int tries = 0;
                    do
                    {
                        target = teammates[_random.Next(teammates.Count)];
                        tries++;
                    } while (picked.Contains(target) && tries < 20);

                    if (picked.Contains(target)) continue;
                    picked.Add(target);

                    int amount = _comboActive ? _random.Next(min, max + 1) * 2 : _random.Next(min, max + 1);
                    target.InGameMoneyServices!.Account += amount;
                    if (target.InGameMoneyServices.Account < 0) target.InGameMoneyServices.Account = 0;
                    Utilities.SetStateChanged(target, "CCSPlayerController", "m_pInGameMoneyServices");
                    target.PrintToChat($" {_localizer["command.prefix"].Value}{_localizer["dice_Bank_payout"].Value.Replace("{amount}", (amount >= 0 ? "+" : "") + amount.ToString()).Replace("{name}", target.PlayerName)}");
                }
            }
        }
    }
}
