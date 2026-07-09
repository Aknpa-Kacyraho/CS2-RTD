using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class HotPotato : DiceBlueprint
    {
        public override string ClassName => "HotPotato";
        public override List<string> Listeners => ["OnTick"];

        private float _lastDamageTime;

        public HotPotato(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        public override void Reset()
        {
            _players.Clear();
            _lastDamageTime = 0;
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;

            float now = (float)Server.CurrentTime;
            if (now - _lastDamageTime < 1f) return;
            _lastDamageTime = now;

            int damage = _config.Dices.HotPotato.Damage;
            float radius = _config.Dices.HotPotato.Radius;

            // Find C4 (dropped or planted)
            Vector? c4Pos = null;

            // First check planted C4
            var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4")
                .FirstOrDefault(c => c.IsValid && c.AbsOrigin != null);
            if (plantedC4 != null)
            {
                c4Pos = plantedC4.AbsOrigin;
            }
            else
            {
                // Check dropped C4 (weapon_c4 in the world)
                var droppedC4List = Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>("weapon_c4")
                    .Where(c => c.IsValid && c.AbsOrigin != null && c.OwnerEntity?.Value == null)
                    .ToList();
                if (droppedC4List.Count > 0)
                {
                    c4Pos = droppedC4List[0].AbsOrigin;
                }
            }

            if (c4Pos == null) return;

            // Damage all players within radius of C4
            foreach (var p in Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV
                    && p.PlayerPawn?.Value != null && p.PlayerPawn.Value.IsValid
                    && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    && p.PlayerPawn.Value.AbsOrigin != null))
            {
                CCSPlayerPawn pawn = p.PlayerPawn!.Value!;
                float dist = Vectors.GetDistance(c4Pos, pawn.AbsOrigin!);
                if (dist <= radius)
                {
                    pawn.Health -= damage;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                    if (pawn.Health <= 0)
                    {
                        if (!p.IsBot)
                            pawn.CommitSuicide(false, true);
                    }
                }
            }
        }
    }
}
