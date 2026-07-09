using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Parasite : DiceBlueprint
    {
        public override string ClassName => "Parasite";
        private bool _comboActive;
        public override List<string> Listeners => ["OnPlayerTakeDamagePre", "OnTick"];
        public override List<string> Events => ["EventPlayerDeath"];

        private readonly Dictionary<ulong, float> _parasiteDamage = [];
        private readonly Dictionary<ulong, HashSet<ulong>> _markedTargets = [];

        public Parasite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
                return;
            _players.Add(player);
            _parasiteDamage[player.SteamID] = 0f;
            _comboActive = DiceSynergy.HasPartner(player, "Plague");
            if (_comboActive)
                DiceSynergy.AnnounceCombo(player, "生化危机", "瘟疫+寄生！传染翻倍+寄生效果翻倍！");
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _parasiteDamage.Remove(player.SteamID);
            _ = _markedTargets.Remove(player.SteamID);
            DamageBonusManager.Unregister(player, "Parasite");
            SpeedBonusManager.Unregister(player, "Parasite");
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
            {
                DamageBonusManager.Unregister(p, "Parasite");
                SpeedBonusManager.Unregister(p, "Parasite");
            }
            _players.Clear();
            _parasiteDamage.Clear();
            _markedTargets.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            CCSPlayerController? attacker = info.Attacker?.Value?.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
                return HookResult.Continue;

            CCSPlayerController? victim = entity.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || attacker == victim)
                return HookResult.Continue;

            ulong holderId = attacker.SteamID;
            if (!_markedTargets.ContainsKey(holderId))
                _markedTargets[holderId] = [];
            _markedTargets[holderId].Add(victim.SteamID);

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null || !victim.IsValid) return HookResult.Continue;

            ulong victimId = victim.SteamID;

            foreach (var (holderId, targets) in _markedTargets.ToList())
            {
                if (!targets.Contains(victimId)) continue;

                targets.Remove(victimId);
                if (targets.Count == 0)
                    _markedTargets.Remove(holderId);

                float currentDmg = _parasiteDamage.GetValueOrDefault(holderId, 0f);
                float dmgCap = _config.Dices.Parasite.DamageCap;
                float dmgStep = _config.Dices.Parasite.DamageBonus * (_comboActive ? 2f : 1f);
                float newDmg = Math.Min(currentDmg + dmgStep, dmgCap);
                _parasiteDamage[holderId] = newDmg;

                DamageBonusManager.RegisterBySteamId(holderId, "Parasite", newDmg);

                float speedCap = _comboActive ? 1f : 0.5f;
                float speedBonus = Math.Min(_config.Dices.Parasite.SpeedBonus * (newDmg / Math.Max(_config.Dices.Parasite.DamageBonus, 0.01f)), speedCap);
                SpeedBonusManager.RegisterBySteamId(holderId, "Parasite", speedBonus);

                // Find the holder and heal them
                var holder = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == holderId);
                if (holder != null && holder.IsValid)
                {
                    CCSPlayerPawn pawn = holder.PlayerPawn?.Value;
                    if (pawn != null && pawn.IsValid)
                    {
                        pawn.MaxHealth = Math.Max(pawn.MaxHealth, pawn.Health + _config.Dices.Parasite.HpRestore);
                        pawn.Health = pawn.Health + _config.Dices.Parasite.HpRestore;
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                    holder.PrintToCenterAlert($"🦠 寄生！+{(int)(newDmg * 100)}%伤害 +{(int)(speedBonus * 100)}%速度！");
                }
            }

            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_players.Count == 0) return;

            foreach (var player in _players.ToList())
            {
                if (player == null || !player.IsValid
                    || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                float effective = SpeedBonusManager.GetEffective(player);
                if (effective > 0)
                {
                    player.PlayerPawn.Value.VelocityModifier = 1 + effective;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }
    }
}
