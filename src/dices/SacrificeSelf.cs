using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class SacrificeSelf : DiceBlueprint
    {
        public override string ClassName => "SacrificeSelf";
        public override List<string> Listeners => ["OnPlayerButtonsChanged", "OnTick", "OnPlayerTakeDamagePre"];
        private readonly HashSet<CCSPlayerController> _boostedTeammates = [];

        public SacrificeSelf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("💝 按E键牺牲自己！全队获得20%伤害+50%速度加成！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var t in _boostedTeammates.ToList())
            {
                SpeedBonusManager.Unregister(t, "SacrificeSelf");
                DamageBonusManager.Unregister(t, "SacrificeSelf");
            }
            _players.Clear();
            _boostedTeammates.Clear();
        }

        public override void Destroy() => Reset();

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            float speedMult = _config.Dices.SacrificeSelf.SpeedMultiplier;
            string playerName = player.PlayerName;

            foreach (var t in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.TeamNum == player.TeamNum && p != player
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                SpeedBonusManager.Register(t, "SacrificeSelf", _config.Dices.SacrificeSelf.SpeedMultiplier - 1.0f);
                DamageBonusManager.Register(t, "SacrificeSelf", _config.Dices.SacrificeSelf.DamageMultiplier - 1.0f);
                _boostedTeammates.Add(t);
                t.PrintToCenterAlert("💝 队友牺牲了！获得+20%伤害+50%速度加成！");
            }

            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_SacrificeSelf_broadcast"].Value.Replace("{playerName}", playerName)}");

            if (!player.IsBot && !player.IsHLTV)
                player.PlayerPawn.Value.CommitSuicide(false, true);
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_boostedTeammates.Contains(attacker))
                return HookResult.Continue;

            if (DamageBonusManager.IsHighest(attacker, "SacrificeSelf"))
            {
                float effective = DamageBonusManager.GetEffective(attacker);
                info.Damage = (int)(info.Damage * (1 + effective));
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_boostedTeammates.Count == 0) return;

            foreach (var player in _boostedTeammates.ToList())
            {
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                float effective = SpeedBonusManager.GetEffective(player);
                if (effective > 0)
                {
                    player.PlayerPawn.Value.VelocityModifier = 1 + effective;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
