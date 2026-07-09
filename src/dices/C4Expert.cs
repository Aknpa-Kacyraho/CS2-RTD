using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class C4Expert : DiceBlueprint
    {
        public override string ClassName => "C4Expert";
        public override List<string> Listeners => ["OnTick"];

        public C4Expert(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                        continue;
                    // CT: instant defuse
                    if (player.Team == CsTeam.CounterTerrorist && plantedC4 != null && plantedC4.IsValid && plantedC4.DefuseCountDown > 1f)
                    {
                        plantedC4.DefuseCountDown = 1f;
                        Utilities.SetStateChanged(plantedC4, "CPlantedC4", "m_fDefuseCountDown");
                    }
                    // T: the bomb plant time is handled by CS2 engine internally;
                    // we speed it up by reducing the C4 timer once planted
                    if (player.Team == CsTeam.Terrorist && plantedC4 != null && plantedC4.IsValid)
                    {
                        plantedC4.C4Blow = plantedC4.C4Blow > 0 ? Math.Min(plantedC4.C4Blow, (float)(Server.CurrentTime + 3)) : 0;
                    }
                }
                catch { }
            }
        }
    }
}
