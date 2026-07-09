using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ShadowWarrior : DiceBlueprint
    {
        public override string ClassName => "ShadowWarrior";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick"];
        private readonly HashSet<CCSPlayerController> _cloaked = [];

        public ShadowWarrior(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);

            // Apply invisibility immediately
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.Render = Color.FromArgb(20, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            _cloaked.Add(player);

            _comboActive = DiceSynergy.HasPartner(player, "Hermit");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "暗影行者", "隐者+影者！隐身透明度更低，走路无脚步声！");

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("👥 影者！几乎完全隐身！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _players.Remove(player);
            _cloaked.Remove(player);
            if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
            }
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                if (p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid)
                {
                    p.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(p.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                }
            }
            _players.Clear();
            _cloaked.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;

            int alpha = _comboActive ? 10 : 25;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                    continue;
                if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                // Maintain invisibility (engine resets on hit/shoot/animation)
                if (pawn.Render.A != alpha)
                {
                    pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
    }
}
