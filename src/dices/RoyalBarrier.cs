using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class RoyalBarrier : DiceBlueprint
    {
        public override string ClassName => "RoyalBarrier";
        public override List<string> Listeners => ["OnTick"];
        private bool _comboActive;

        public RoyalBarrier(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            pawn.MaxHealth = _config.Dices.RoyalBarrier.MaxHealth;
            pawn.Health = _config.Dices.RoyalBarrier.MaxHealth;
            pawn.ArmorValue = _config.Dices.RoyalBarrier.MaxArmor;
            // Combo: Giant + RoyalBarrier = "钢铁要塞" — speed penalty halved, can jump
            _comboActive = DiceSynergy.HasPartner(player, "Giant");
            float speedMult = _comboActive ? 0.65f : _config.Dices.RoyalBarrier.SpeedMultiplier;

            pawn.VelocityModifier = speedMult;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "钢铁要塞", "巨人撑起堡垒！移速提升至 65%，跳跃恢复");

            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName }, { "health", _config.Dices.RoyalBarrier.MaxHealth.ToString() }, { "armor", _config.Dices.RoyalBarrier.MaxArmor.ToString() } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
                Remove(player);
            _players.Clear();
        }

        public override void Destroy() => Reset();

        public void OnTick()
        {
            if (_players.Count == 0) return;

            int maxHealth = _config.Dices.RoyalBarrier.MaxHealth;
            float speedMult = _comboActive ? 0.65f : _config.Dices.RoyalBarrier.SpeedMultiplier;
            bool blockJump = !_comboActive;
            foreach (CCSPlayerController player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid
                        || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                        || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                        continue;

                    CCSPlayerPawn pawn = player.PlayerPawn.Value;

                    // Maintain MaxHealth (engine may clamp it)
                    if (pawn.MaxHealth != maxHealth)
                    {
                        pawn.MaxHealth = maxHealth;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    }

                    // Maintain VelocityModifier
                    if (Math.Abs(pawn.VelocityModifier - speedMult) > 0.01f)
                    {
                        pawn.VelocityModifier = speedMult;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    }

                    // Block jumping (disabled when combo active with Giant)
                    if (blockJump && pawn.AbsVelocity.Z > 50f)
                    {
                        pawn.Teleport(pawn.AbsOrigin, pawn.AbsRotation, new Vector(
                            pawn.AbsVelocity.X, pawn.AbsVelocity.Y, 0));
                    }
                }
                catch { }
            }
        }
    }
}
