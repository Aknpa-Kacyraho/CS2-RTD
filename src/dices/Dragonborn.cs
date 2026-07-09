using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class Dragonborn : DiceBlueprint
    {
        public override string ClassName => "Dragonborn";
        private bool _comboActive;
        private bool _hasDragonSoul;
        private bool _hasRespawn;
        public override List<string> Events => ["EventPlayerDeath"];
        public override List<string> Listeners => ["OnPlayerButtonsChanged"];
        private readonly Dictionary<CCSPlayerController, int> _killCount = [];
        private readonly Dictionary<CCSPlayerController, bool> _transformed = [];

        public Dragonborn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;
            _players.Add(player);
            _hasDragonSoul = DiceSynergy.HasPartner(player, "DragonSoul");
            _hasRespawn = DiceSynergy.HasPartner(player, "Respawn");
            _comboActive = _hasDragonSoul || _hasRespawn;
            if (_hasRespawn)
                DiceSynergy.AnnounceCombo(player, "浴火重生", "浴火重生联动生效！重生后200HP 200护甲！");
            if (_hasDragonSoul)
                DiceSynergy.AnnounceCombo(player, "巨龙共鸣", "巨龙之魂+龙裔！化龙时获得龙魂之力，HP+150！");
            _killCount[player] = 0;
            _transformed[player] = false;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
            player.PrintToCenterAlert("🐉 龙裔！击杀2人后按E化身为龙！");
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _killCount.Remove(player);
            _ = _transformed.Remove(player);
        }

        public override void Reset() { _players.Clear(); _killCount.Clear(); _transformed.Clear(); }

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid || !_players.Contains(attacker)) return HookResult.Continue;
            if (attacker == @event.Userid) return HookResult.Continue;
            if (_transformed.TryGetValue(attacker, out bool done) && done) return HookResult.Continue;

            int current = _killCount.TryGetValue(attacker, out int k) ? k : 0;
            current++;
            _killCount[attacker] = current;

            int required = _config.Dices.Dragonborn.KillsRequired;
            if (current >= required)
                attacker.PrintToCenterAlert("🐉 龙之血已满！按 E 化身为龙！");
            else
                attacker.PrintToCenterAlert($"🐉 龙之血 ({current}/{required})");

            return HookResult.Continue;
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_players.Count == 0) return;
            if (player == null || !player.IsValid || !_players.Contains(player)) return;
            if (!pressed.HasFlag(PlayerButtons.Use)) return;
            if (_transformed.TryGetValue(player, out bool done) && done) return;

            int current = _killCount.TryGetValue(player, out int k) ? k : 0;
            int required = _config.Dices.Dragonborn.KillsRequired;
            if (current < required) return;
            if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid) return;

            _transformed[player] = true;
            CCSPlayerPawn pawn = player.PlayerPawn.Value;
            bool isDead = pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE;
            CCSPlayerController captured = player;

            // DragonSoul combo: if THIS player has DragonSoul, transform into IceDragon or FireDragon
            bool hasDragonSoulSelf = _hasDragonSoul && RollTheDice.Instance?.HasDiceActive(captured, "DragonSoul") == true;
            if (hasDragonSoulSelf)
            {
                var instance = RollTheDice.Instance;
                if (instance != null)
                {
                    string dragonType = Random.Shared.Next(2) == 0 ? "IceDragon" : "FireDragon";
                    Server.NextFrame(() =>
                    {
                        if (captured == null || !captured.IsValid) return;
                        instance.RemoveDiceFromPlayer(captured, "Dragonborn");
                        instance.RemoveDiceFromPlayer(captured, "DragonSoul");
                        Server.NextFrame(() =>
                        {
                            if (captured == null || !captured.IsValid) return;
                            // Respawn if dead before giving new dice
                            if (captured.PlayerPawn?.Value != null
                                && captured.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                            {
                                captured.Respawn();
                                Server.NextFrame(() =>
                                {
                                    instance.ForceDiceForPlayer(captured, dragonType);
                                    Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🐉🔥 {captured.PlayerName} 龙魂共鸣，化身为{(dragonType == "IceDragon" ? "冰巨龙" : "火巨龙")}！");
                                });
                            }
                            else
                            {
                                instance.ForceDiceForPlayer(captured, dragonType);
                                Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🐉🔥 {captured.PlayerName} 龙魂共鸣，化身为{(dragonType == "IceDragon" ? "冰巨龙" : "火巨龙")}！");
                            }
                        });
                    });
                    captured.PrintToCenterAlert("🐉 龙魂共鸣！化身为龙！");
                    return;
                }
            }

            int hp = _config.Dices.Dragonborn.DragonHP;
            int armor = _config.Dices.Dragonborn.DragonArmor;
            if (_hasDragonSoul) { hp += 150; armor += 150; } // Teammate DragonSoul: extra dragon power

            if (isDead)
            {
                // Dead: Respawn first, then apply dragon stats
                Server.NextFrame(() =>
                {
                    Server.NextFrame(() =>
                    {
                        if (captured?.PlayerPawn?.Value == null
                            || captured.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                            return;

                        captured.Respawn();

                        Server.NextFrame(() =>
                        {
                            if (captured?.PlayerPawn?.Value == null
                                || captured.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                                return;

                            CCSPlayerPawn p = captured.PlayerPawn.Value;
                            p.MaxHealth = hp;
                            p.Health = hp;
                            p.ArmorValue = armor;
                            Utilities.SetStateChanged(p, "CBaseEntity", "m_iMaxHealth");
                            Utilities.SetStateChanged(p, "CBaseEntity", "m_iHealth");
                            Utilities.SetStateChanged(p, "CCSPlayerPawn", "m_ArmorValue");
                        });
                    });
                });
            }
            else
            {
                // Alive: Apply dragon stats directly
                pawn.MaxHealth = hp;
                pawn.Health = hp;
                pawn.ArmorValue = armor;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            }

            captured.PrintToCenterAlert(_hasDragonSoul ? "🐉 龙魂共鸣！450HP 450护甲！" : "🐉 化身为龙！300HP 300护甲！");
            Server.PrintToChatAll($" {_localizer["command.prefix"].Value}{_localizer["dice_Dragonborn_transform"].Value.Replace("{playerName}", captured.PlayerName)}");
        }
    }
}
