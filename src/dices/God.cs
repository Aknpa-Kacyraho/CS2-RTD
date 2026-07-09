using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class God : DiceBlueprint
    {
        public override string ClassName => "God";
        private bool _comboActive;
        public override List<string> Listeners => ["OnTick", "OnPlayerTakeDamagePre"];

        private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = [];
        private readonly Dictionary<CCSPlayerController, int> _originalArmor = [];

        public God(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            CCSPlayerPawn pawn = player.PlayerPawn.Value;

            _originalMaxHealth[player] = pawn.MaxHealth;
            _originalArmor[player] = pawn.ArmorValue;

            pawn.MaxHealth = 666;
            pawn.Health = 666;
            pawn.ArmorValue = 666;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            pawn.VelocityModifier = 2.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            _players.Add(player);
            _comboActive = DiceSynergy.HasPartner(player, "Goddess");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "神之共鸣", "上帝伤害翻倍+女神多祝福一人！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
            {
                CCSPlayerPawn pawn = player.PlayerPawn.Value;
                if (_originalMaxHealth.TryGetValue(player, out int origHp))
                {
                    pawn.MaxHealth = origHp;
                    pawn.Health = Math.Min(pawn.Health, origHp);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    _originalMaxHealth.Remove(player);
                }
                if (_originalArmor.TryGetValue(player, out int origArmor))
                {
                    pawn.ArmorValue = Math.Min(pawn.ArmorValue, origArmor);
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                    _originalArmor.Remove(player);
                }
                pawn.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var player in _players.ToList()) Remove(player);
            _players.Clear();
            _originalMaxHealth.Clear();
            _originalArmor.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (_players.Count == 0) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker != null && attacker.IsValid && _players.Contains(attacker))
            {
                info.Damage *= (_comboActive ? 3f : 1.5f);
                return HookResult.Changed;
            }
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;
            foreach (var player in _players.ToList())
            {
                try
                {
                    if (player == null || !player.IsValid || player.PlayerPawn?.Value == null
                        || !player.PlayerPawn.Value.IsValid) continue;
                    CCSPlayerPawn pawn = player.PlayerPawn.Value;
                    if (pawn.VelocityModifier < 1.5f)
                    {
                        pawn.VelocityModifier = 2.0f;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    }
                }
                catch { }
            }
        }
    }
}
