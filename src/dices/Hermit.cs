using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Hermit : DiceBlueprint
    {
        public override string ClassName => "Hermit";
        private readonly Dictionary<CCSPlayerController, bool> _comboActive = [];
        public override List<string> Listeners => ["OnTick"];
        public override Dictionary<int, HookMode> UserMessages => new()
        {
            { 208, HookMode.Pre }
        };

        public Hermit(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            bool hasCombo = DiceSynergy.HasPartner(player, "ShadowWarrior");
            _comboActive[player] = hasCombo;
            if (hasCombo)
                DiceSynergy.AnnounceCombo(player, "暗影行者", "极限隐身！");

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            int alpha = hasCombo ? 10 : 30;
            pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _comboActive.Remove(player);
            if (player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                if (p != null && p.IsValid && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                }
            }
            _players.Clear();
            _comboActive.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                int alpha = _comboActive.TryGetValue(player, out bool hasCombo) && hasCombo ? 10 : 30;

                player.PlayerPawn.Value.Render = Color.FromArgb(alpha, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
        }

        public HookResult HookUserMessage208(UserMessage um)
        {
            int entityIndex = um.ReadInt("source_entity_index");

            foreach (CCSPlayerController player in _players)
            {
                if (player?.PlayerPawn?.Value != null
                    && player.PlayerPawn.Value.IsValid
                    && player.PlayerPawn.Value.Index == entityIndex)
                {
                    um.Recipients.Clear();
                    return HookResult.Stop;
                }
            }

            return HookResult.Continue;
        }
    }
}
