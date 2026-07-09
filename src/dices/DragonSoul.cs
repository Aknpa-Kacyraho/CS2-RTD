using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DragonSoul : DiceBlueprint
    {
        public override string ClassName => "DragonSoul";
        private bool _comboActive;

        // === v3.0 special dice properties ===
        public override float Weight => 1.0f;
        public override bool IsSpecial => true;
        public override float SecondRoundProbability => 0.1f;
        // SecondRoundRewardId remains null (default) — round 2 winners get DragonSoul itself

        public override List<string> Listeners => ["OnTick"];

        private static bool _transforming;
        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];

        public DragonSoul(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            _originalMaxHealth[player] = pawn.MaxHealth;
            pawn.MaxHealth = pawn.MaxHealth + _config.Dices.DragonSoul.BonusHP;
            pawn.Health = Math.Min(pawn.Health + _config.Dices.DragonSoul.BonusHP, pawn.MaxHealth);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Dragonborn");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "巨龙共鸣", "巨龙之魂+龙裔！龙裔化龙时额外获得龙魂之力！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                if (_originalMaxHealth.TryGetValue(player, out int origMax))
                {
                    pawn.MaxHealth = origMax;
                    pawn.Health = Math.Min(pawn.Health, origMax);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    _originalMaxHealth.Remove(player);
                }
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList()) Remove(p);
            _players.Clear();
            _originalMaxHealth.Clear();
            _transforming = false;
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count < 2 || _transforming) return;

            var groupedByTeam = _players
                .Where(p => p != null && p.IsValid && p.TeamNum is 2 or 3
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .GroupBy(p => p.TeamNum)
                .Where(g => g.Count() >= 2);

            foreach (var teamGroup in groupedByTeam)
            {
                var list = teamGroup.ToList();
                if (list.Count < 2) continue;

                _transforming = true;

                CCSPlayerController first = list[0];
                CCSPlayerController second = list[1];

                var instance = RollTheDice.Instance;
                if (instance == null) { _transforming = false; return; }

                instance.RemoveDiceFromPlayer(first, "DragonSoul");
                instance.RemoveDiceFromPlayer(second, "DragonSoul");

                Server.NextFrame(() =>
                {
                    instance.ForceDiceForPlayer(first, "IceDragon");
                    instance.ForceDiceForPlayer(second, "FireDragon");
                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🧊🔥 {first.PlayerName} 和 {second.PlayerName} 的巨龙之魂共鸣！分别进化为冰巨龙与火巨龙！");
                    _transforming = false;
                });
            }
        }
    }
}
