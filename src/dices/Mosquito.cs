using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Mosquito : DiceBlueprint
    {
        public override string ClassName => "Mosquito";
        private bool _comboActive;
        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];

        public Mosquito(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

            // Store original max health
            _originalMaxHealth[player] = pawn.MaxHealth;

            float size = _config.Dices.Mosquito.SizeScale;
            pawn.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = size;
            pawn.AcceptInput("SetScale", null, null, size.ToString());
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");

            int newHp = _config.Dices.Mosquito.Health;
            pawn.MaxHealth = newHp;
            pawn.Health = newHp;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            _players.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "PlayAsChicken");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "迷你鸡神", "蚊子HP翻倍 鸡神HP翻倍");
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
            if (_originalMaxHealth.TryGetValue(player, out int originalMax))
            {
                if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
                {
                    player.PlayerPawn.Value.MaxHealth = originalMax;
                    player.PlayerPawn.Value.Health = Math.Min(player.PlayerPawn.Value.Health, originalMax);
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                }
                _ = _originalMaxHealth.Remove(player);
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList())
                Remove(player);
            _players.Clear();
            _originalMaxHealth.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }
    }
}
