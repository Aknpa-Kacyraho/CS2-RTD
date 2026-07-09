using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils
{
    /// <summary>
    /// Lightweight dice synergy (combo) system. When two specific dice are active on
    /// the same player, each participating dice can apply enhanced effects, and the
    /// player's name prefix merges the two dice labels into a single combo name.
    /// </summary>
    public static class DiceSynergy
    {
        /// <summary>Combo registry: (diceA, diceB) → combo display name. Order-independent.</summary>
        private static readonly List<(string A, string B, string ComboName)> _combos =
        [
            ("Giant",              "RoyalBarrier",          "钢铁要塞"),
            ("Vampire",            "SoulEater",             "噬魂血族"),
            ("Fireball",           "FireLord",              "焚天灭地"),
            ("Berserker",          "Adrenaline",            "狂暴血脉"),
            ("Berserker",          "DamageMultiplier",      "狂暴之力"),
            ("Regeneration",       "JumpHeal",              "生命律动"),
            ("Priest",             "Pope",                  "神圣共鸣"),
            ("Cutter",             "SwordSaint",            "剑刃风暴"),
            ("Evasion",            "Shield",                "钢铁壁垒"),
            ("Emperor",            "Empress",               "王权永恒"),
            ("Dragonborn",         "Respawn",               "浴火重生"),
            ("Necromancer",        "InfiniteProliferation", "不死军团"),
            ("SniperElite",        "DeagleKing",            "精准猎杀"),
            ("Bank",               "Miser",                 "资本要塞"),
            ("Countdown",          "Rewind",                "时空主宰"),
            ("Thorns",             "GuardianAngel",         "圣光荆棘"),
            ("Nirvana",            "GuardianAngel",         "菲尼克斯"),

            ("IceBeam",            "PoisonBlade",           "霜毒双刃"),
            ("Bounty",             "Capitalist",            "赏金猎人"),
            ("Evolution",          "Awakener",              "超进化"),
            ("Hermit",             "ShadowWarrior",         "暗影行者"),
            ("GrenadeKing",        "Martyrdom",             "爆炸艺术家"),
            ("Gargoyle",           "Titanfall",             "泰坦神像"),
            ("LaserCage",          "ThunderChain",          "雷光炼狱"),
            ("Drone",              "RepulsionField",        "无人防线"),
            ("FrontlineBeast",     "SpeedOnKill",           "猎杀本能"),
            ("Karma",              "Plague",                "因果循环"),
            ("Mosquito",           "PlayAsChicken",         "迷你鸡神"),
            ("World",              "Heaven",                "超越天堂"),
            ("Izayoi",             "Heaven",                "超越天堂"),
            ("DeathKnight",        "Frostmourne",           "死亡骑士完全体"),
            ("PistolMaster",       "Disarm",                "缴械大师"),


            ("SmokeBomb",          "SmokeVision",           "烟雾掌控"),
            ("BlackHole",          "GravityWell",           "引力深渊"),
            ("BlackHole",          "WhiteHole",             "坍缩"),
            ("Overheat",           "Adrenaline",            "狂热"),
            ("Bugle",              "World",                 "天启"),
            ("Cthulhu",            "DuskDawn",              "深渊觉醒"),
            ("God",                "Goddess",               "神之共鸣"),
            ("Parasite",           "Plague",                "生化危机"),
            ("Drone",              "Satellite",             "天网"),
            ("IceBeam",            "Amber",                 "极寒地狱"),
            ("Ragnarok",           "NukeLeak",              "末日审判"),
            ("DeadHand",           "DeagleKing",            "致命一击"),
            ("DragonSoul",         "Dragonborn",            "巨龙共鸣"),
            ("MagneticPulse",      "Thorns",                "磁力荆棘"),
            ("MagneticPulse",      "RepulsionField",        "禁区"),
        ];

        /// <summary>
        /// Given a set of active dice class names, replace matched pairs with their
        /// combo name. Unchanged dice names are returned as-is (already localized).
        /// </summary>
        public static HashSet<string> ResolveComboNames(HashSet<string> activeDice)
        {
            var result = new HashSet<string>(activeDice);
            var consumed = new HashSet<string>();

            foreach (var (a, b, comboName) in _combos)
            {
                if (result.Contains(a) && result.Contains(b))
                {
                    result.Remove(a);
                    result.Remove(b);
                    result.Add(comboName);
                    consumed.Add(a);
                    consumed.Add(b);
                }
            }

            return result;
        }

        /// <summary>Check if the player or any teammate also has <paramref name="partnerClassName"/> active.</summary>
        public static bool HasPartner(CCSPlayerController player, string partnerClassName)
        {
            var instance = global::RollTheDice.RollTheDice.Instance;
            if (instance == null) return false;
            // Check self
            if (instance.HasDiceActive(player, partnerClassName)) return true;
            // Check teammates
            return Utilities.GetPlayers().Any(p =>
                p.IsValid && p != player && p.TeamNum == player.TeamNum
                && instance.HasDiceActive(p, partnerClassName));
        }

        /// <summary>Broadcast a combo activation message to all players.</summary>
        public static void AnnounceCombo(CCSPlayerController player, string comboName, string comboMsg)
        {
            Server.PrintToChatAll(
                $" \x04[Combo!]\x01 \x02{player.PlayerName}\x01 触发了 \x03{comboName}\x01：{comboMsg}");
        }
    }
}
