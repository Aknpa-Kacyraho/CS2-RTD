using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class DeadHand : DiceBlueprint
    {
        public override string ClassName => "DeadHand";
        private bool _comboActive;
        public override List<string> Events => ["EventWeaponFire"];
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];

        public DeadHand(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "DeagleKing");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "致命一击", "开枪自伤减半+命中回血翻倍！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_DeadHand_broadcast"].Value.Replace("{playerName}", player.PlayerName)}");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public override void Reset() => _players.Clear();
        public override void Destroy() => Reset();

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;

            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid
                || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return HookResult.Continue;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            int selfDamage = _comboActive ? _config.Dices.DeadHand.SelfDamage / 2 : _config.Dices.DeadHand.SelfDamage;

            pawn.Health = Math.Max(0, pawn.Health - selfDamage);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            if (pawn.Health <= 0)
            {
                if (!player.IsBot && !player.IsHLTV)
                    pawn.CommitSuicide(false, true);
                else
                {
                    try { pawn.CommitSuicide(false, true); }
                    catch
                    {
                        pawn.Health = 0;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                }
            }

            return HookResult.Continue;
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = attacker.PlayerPawn?.Value;
            if (attackerPawn == null || !attackerPawn.IsValid) return HookResult.Continue;

            int heal = _config.Dices.DeadHand.SelfDamage * (_comboActive ? 2 : 1);
            attackerPawn.Health = Math.Min(attackerPawn.Health + heal, attackerPawn.MaxHealth);
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");

            return HookResult.Continue;
        }
    }
}
