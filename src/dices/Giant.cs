using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Giant : DiceBlueprint
    {
        public override string ClassName => "Giant";
        private bool _comboActive;

        public Giant(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid
                || player.PlayerPawn?.Value?.CBodyComponent?.SceneNode?.GetSkeletonInstance() == null)
            {
                return;
            }

            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            float size = _config.Dices.Giant.SizeScale;
            pawn.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = size;
            pawn.AcceptInput("SetScale", null, null, size.ToString());
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");

            // Use stacking health to avoid snapshot corruption
            float hpMult = _config.Dices.Giant.HealthMultiplier;
            StackingHealth.RegisterMultiplier(player, "Giant", hpMult);
            pawn.Health = pawn.MaxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            // Combo: Giant + RoyalBarrier = "钢铁要塞" — extra armor
            if (DiceSynergy.HasPartner(player, "RoyalBarrier"))
            {
                _comboActive = true;
                pawn.ArmorValue = Math.Max(pawn.ArmorValue, 100);
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                DiceSynergy.AnnounceCombo(player, "钢铁要塞", "巨人体魄撑起堡垒！+100 护甲");
            }

            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player?.Pawn?.Value?.CBodyComponent?.SceneNode != null
                && player.Pawn.Value.IsValid)
            {
                player.Pawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 1.0f;
                player.Pawn.Value.AcceptInput("SetScale", null, null, "1");
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_CBodyComponent");
            }
            StackingHealth.UnregisterMultiplier(player, "Giant");
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
                Remove(player);
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }
    }
}
