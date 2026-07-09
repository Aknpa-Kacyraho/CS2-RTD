using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Knight : DiceBlueprint
    {
        public override string ClassName => "Knight";
        public override List<string> Listeners => [
            "OnTick"
        ];
        private readonly Dictionary<CCSPlayerController, float> _nextTransferTime = [];

        public Knight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            _originalMaxHealth[player] = pawn.MaxHealth;
            pawn.MaxHealth = _config.Dices.Knight.SelfMaxHealth;
            pawn.Health = _config.Dices.Knight.SelfMaxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            _players.Add(player);
            _nextTransferTime[player] = 0f;
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _nextTransferTime.Remove(player);
            if (_originalMaxHealth.TryGetValue(player, out int originalMaxHealth)
                && player?.PlayerPawn?.Value != null
                && player.PlayerPawn.Value.IsValid)
            {
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                pawn.MaxHealth = originalMaxHealth;
                if (pawn.Health > pawn.MaxHealth)
                {
                    pawn.Health = pawn.MaxHealth;
                }
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            }
            _ = _originalMaxHealth.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
            _nextTransferTime.Clear();
            _originalMaxHealth.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public void OnTick()
        {
            if (_nextTransferTime.Count == 0) return;

            float now = (float)Server.CurrentTime;
            float interval = _config.Dices.Knight.TransferInterval;
            int hpTransfer = _config.Dices.Knight.HpTransfer;

            foreach (CCSPlayerController knight in _players.ToList())
            {
                try
                {
                    if (knight == null
                        || !knight.IsValid
                        || knight.PlayerPawn?.Value == null
                        || !knight.PlayerPawn.Value.IsValid
                        || knight.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        continue;
                    }

                    if (!_nextTransferTime.TryGetValue(knight, out float nextTime) || nextTime > now)
                    {
                        continue;
                    }

                    CCSPlayerPawn? knightPawn = knight.PlayerPawn.Value;
                    if (knightPawn.Health <= 1) continue;

                    CsTeam knightTeam = knight.Team;
                    int totalTransferred = 0;

                    foreach (CCSPlayerController teammate in Utilities.GetPlayers()
                        .Where(p => p != knight
                            && p.IsValid
                            && p.Team == knightTeam
                            && p.PlayerPawn?.Value != null
                            && p.PlayerPawn.Value.IsValid
                            && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                            && !_players.Contains(p)))
                    {
                        if (knightPawn.Health <= 1) break;

                        CCSPlayerPawn? teammatePawn = teammate.PlayerPawn!.Value;
                        if (teammatePawn.Health >= teammatePawn.MaxHealth) continue;

                        int needed = teammatePawn.MaxHealth - teammatePawn.Health;
                        int give = Math.Min(hpTransfer, needed);
                        give = Math.Min(give, knightPawn.Health - 1);
                        if (give <= 0) continue;

                        teammatePawn.Health += give;
                        Utilities.SetStateChanged(teammatePawn, "CBaseEntity", "m_iHealth");
                        totalTransferred += give;
                    }

                    if (totalTransferred > 0)
                    {
                        knightPawn.Health -= totalTransferred;
                        Utilities.SetStateChanged(knightPawn, "CBaseEntity", "m_iHealth");
                    }

                    _nextTransferTime[knight] = now + interval;
                }
                catch
                {
                    _nextTransferTime.Remove(knight);
                }
            }
        }
    }
}
