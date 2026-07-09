using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class FogOfWar : DiceBlueprint
    {
        public override string ClassName => "FogOfWar";
        public override List<string> Listeners => ["OnTick"];

        private CFogController? _fogController;
        private CPlayerVisibility? _playerVisibility;
        private readonly HashSet<uint> _foggedPawnIndices = [];

        public FogOfWar(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            _players.Add(player);

            // Create/ensure fog controller
            EnsureFogEntities();

            // Apply fog to all players
            ApplyFogToAll();

            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            if (_players.Count == 0)
                CleanupFog();
        }

        public override void Reset()
        {
            _players.Clear();
            _foggedPawnIndices.Clear();
            CleanupFog();
        }

        public override void Destroy()
        {
            Reset();
        }

        private void EnsureFogEntities()
        {
            // Validate or recreate fog controller
            if (_fogController == null || !_fogController.IsValid)
            {
                _fogController = Utilities.CreateEntityByName<CFogController>("env_fog_controller");
                if (_fogController == null) return;
                _fogController.DispatchSpawn();

                Color color = ColorTranslator.FromHtml(_config.Dices.FogOfWar.Color);
                if (color == Color.Empty) color = Color.DarkOrange;

                _fogController.Fog.Enable = true;
                _fogController.Fog.ColorPrimary = color;
                _fogController.Fog.Exponent = _config.Dices.FogOfWar.Exponent;
                _fogController.Fog.Maxdensity = _config.Dices.FogOfWar.Density;
                _fogController.Fog.End = _config.Dices.FogOfWar.EndDistance;
            }

            // Validate or recreate player visibility
            if (_playerVisibility == null || !_playerVisibility.IsValid)
            {
                _playerVisibility = Utilities.FindAllEntitiesByDesignerName<CPlayerVisibility>("env_player_visibility").FirstOrDefault();
                if (_playerVisibility == null)
                {
                    _playerVisibility = Utilities.CreateEntityByName<CPlayerVisibility>("env_player_visibility");
                    if (_playerVisibility != null) _playerVisibility.DispatchSpawn();
                }
                if (_playerVisibility != null)
                {
                    _playerVisibility.FogMaxDensityMultiplier = _config.Dices.FogOfWar.PlayerVisibility;
                    Utilities.SetStateChanged(_playerVisibility, "CPlayerVisibility", "m_flFogMaxDensityMultiplier");
                }
            }
        }

        private void ApplyFogToAll()
        {
            if (_fogController == null || !_fogController.IsValid) return;
            _foggedPawnIndices.Clear();

            foreach (CCSPlayerController p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                p.PlayerPawn.Value.AcceptInput("SetFogController", _fogController, _fogController, "!activator");
                _foggedPawnIndices.Add(p.PlayerPawn.Value.Index);
            }
        }

        private void CleanupFog()
        {
            _foggedPawnIndices.Clear();
            if (_fogController != null && _fogController.IsValid)
                _fogController.Remove();
            _fogController = null;
            _playerVisibility = null;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            if (Server.TickCount % 32 != 0) return;

            // Re-create fog entities if they became invalid
            if (_fogController == null || !_fogController.IsValid)
            {
                EnsureFogEntities();
                ApplyFogToAll();
                return;
            }

            // Re-apply fog to all alive players every ~0.5s
            // Also catch newly spawned/respawned players
            foreach (CCSPlayerController p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                uint pawnIdx = p.PlayerPawn.Value.Index;
                if (!_foggedPawnIndices.Contains(pawnIdx))
                {
                    p.PlayerPawn.Value.AcceptInput("SetFogController", _fogController, _fogController, "!activator");
                    _foggedPawnIndices.Add(pawnIdx);
                }
            }
        }
    }
}
