using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Redemption : DiceBlueprint
    {
        public override string ClassName => "Redemption";
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnPlayerTakeDamagePre"];
        private readonly Dictionary<CCSPlayerController, List<CCSPlayerController>> _killedBy = [];

        public Redemption(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);

            float bonus = _config.Dices.Redemption.DamageBonus;
            DamageBonusManager.Register(player, "Redemption", bonus);

            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "bonus", (bonus * 100).ToString("F0") }
            });
            player.PrintToCenterAlert($"✝ 救赎！伤害+{bonus * 100:F0}%，死后复活你杀过的人！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            DamageBonusManager.Unregister(player, "Redemption");
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (var p in _players.ToList())
                DamageBonusManager.Unregister(p, "Redemption");
            _players.Clear();
            _killedBy.Clear();
        }

        public override void Destroy() => Reset();

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (info.Attacker.Value == null) return HookResult.Continue;
            CCSPlayerController? attacker = info.Attacker.Value.As<CCSPlayerPawn>()?.Controller?.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;

            float effective = DamageBonusManager.GetEffective(attacker, 0.5f);
            float multiplier = 1f + effective;
            info.Damage *= multiplier;
            return HookResult.Changed;
        }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid) return HookResult.Continue;
            if (attacker == victim) return HookResult.Continue;

            if (!_killedBy.ContainsKey(attacker))
                _killedBy[attacker] = [];
            _killedBy[attacker].Add(victim);

            if (_players.Contains(victim) && _killedBy.TryGetValue(victim, out var killedEnemies))
            {
                foreach (var dead in killedEnemies)
                {
                    if (dead == null || !dead.IsValid) continue;
                    if (dead.PlayerPawn?.Value == null || dead.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE) continue;

                    CCSPlayerController capturedDead = dead;
                    Server.NextFrame(() =>
                    {
                        Server.NextFrame(() =>
                        {
                            if (capturedDead?.PlayerPawn?.Value == null
                                || capturedDead.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                                return;
                            capturedDead.Respawn();
                            Server.NextFrame(() =>
                            {
                                if (capturedDead?.PlayerPawn?.Value == null
                                    || capturedDead.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                                    return;
                                capturedDead.RemoveWeapons();
                                capturedDead.GiveNamedItem("weapon_knife");
                                capturedDead.PlayerPawn.Value.ArmorValue = 100;
                                Utilities.SetStateChanged(capturedDead.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue");
                                capturedDead.PrintToCenterAlert("\u271d 赎罪！敌人复活了你");
                            });
                        });
                    });
                }

                victim.PrintToCenterAlert("\u271d 赎罪！你杀死的人复活了");
                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Redemption_broadcast"].Value.Replace("{playerName}", victim.PlayerName)}");
                _killedBy.Remove(victim);
            }

            return HookResult.Continue;
        }
    }
}
