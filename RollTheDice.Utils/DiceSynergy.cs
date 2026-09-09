using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

public static class DiceSynergy
{
	internal static readonly List<(string A, string B, string ComboName)> _combos;

	public static HashSet<string> ResolveComboNames(HashSet<string> activeDice)
	{
		HashSet<string> hashSet = new HashSet<string>(activeDice);
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (var (item, item2, item3) in _combos)
		{
			if (hashSet.Contains(item) && hashSet.Contains(item2))
			{
				hashSet.Remove(item);
				hashSet.Remove(item2);
				hashSet.Add(item3);
				hashSet2.Add(item);
				hashSet2.Add(item2);
			}
		}
		return hashSet;
	}

	public static bool HasPartner(CCSPlayerController player, string partnerClassName)
	{
		RollTheDice instance = RollTheDice.Instance;
		if (instance == null)
		{
			return false;
		}
		if (instance.HasDiceActive(player, partnerClassName))
		{
			return true;
		}
		return Utilities.GetPlayers().Any((CCSPlayerController p) => ((CEntityInstance)p).IsValid && (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum && instance.HasDiceActive(p, partnerClassName));
	}

	public static void AnnounceCombo(CCSPlayerController player, string comboName, string comboMsg)
	{
		Server.PrintToChatAll($" \u0004[Combo!]\u0001 \u0002{((CBasePlayerController)player).PlayerName}\u0001 触发了 \u0003{comboName}\u0001：{comboMsg}");
	}

	static DiceSynergy()
	{
		int num = 47;
		List<(string, string, string)> list = new List<(string, string, string)>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<(string, string, string)> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = ("Giant", "RoyalBarrier", "钢铁要塞");
		num2++;
		span[num2] = ("Vampire", "SoulEater", "噬魂血族");
		num2++;
		span[num2] = ("Fireball", "FireLord", "焚天灭地");
		num2++;
		span[num2] = ("Berserker", "Adrenaline", "狂暴血脉");
		num2++;
		span[num2] = ("Berserker", "DamageMultiplier", "狂暴之力");
		num2++;
		span[num2] = ("Regeneration", "JumpHeal", "生命律动");
		num2++;
		span[num2] = ("Priest", "Pope", "神圣共鸣");
		num2++;
		span[num2] = ("Cutter", "SwordSaint", "剑刃风暴");
		num2++;
		span[num2] = ("Evasion", "Shield", "钢铁壁垒");
		num2++;
		span[num2] = ("Emperor", "Empress", "王权永恒");
		num2++;
		span[num2] = ("Dragonborn", "Respawn", "浴火重生");
		num2++;
		span[num2] = ("Necromancer", "InfiniteProliferation", "不死军团");
		num2++;
		span[num2] = ("SniperElite", "DeagleKing", "精准猎杀");
		num2++;
		span[num2] = ("Bank", "Miser", "资本要塞");
		num2++;
		span[num2] = ("Countdown", "Rewind", "时空主宰");
		num2++;
		span[num2] = ("Thorns", "GuardianAngel", "圣光荆棘");
		num2++;
		span[num2] = ("Nirvana", "GuardianAngel", "菲尼克斯");
		num2++;
		span[num2] = ("IceBeam", "PoisonBlade", "霜毒双刃");
		num2++;
		span[num2] = ("Bounty", "Capitalist", "赏金猎人");
		num2++;
		span[num2] = ("Evolution", "Awakener", "超进化");
		num2++;
		span[num2] = ("Hermit", "ShadowWarrior", "暗影行者");
		num2++;
		span[num2] = ("GrenadeKing", "Martyrdom", "爆炸艺术家");
		num2++;
		span[num2] = ("Gargoyle", "Titanfall", "泰坦神像");
		num2++;
		span[num2] = ("LaserCage", "ThunderChain", "雷光炼狱");
		num2++;
		span[num2] = ("Drone", "RepulsionField", "无人防线");
		num2++;
		span[num2] = ("FrontlineBeast", "SpeedOnKill", "猎杀本能");
		num2++;
		span[num2] = ("Karma", "Plague", "因果循环");
		num2++;
		span[num2] = ("Mosquito", "PlayAsChicken", "迷你鸡神");
		num2++;
		span[num2] = ("World", "Heaven", "超越天堂");
		num2++;
		span[num2] = ("Izayoi", "Heaven", "超越天堂");
		num2++;
		span[num2] = ("DeathKnight", "Frostmourne", "死亡骑士完全体");
		num2++;
		span[num2] = ("PistolMaster", "Disarm", "缴械大师");
		num2++;
		span[num2] = ("SmokeBomb", "SmokeVision", "烟雾掌控");
		num2++;
		span[num2] = ("BlackHole", "GravityWell", "引力深渊");
		num2++;
		span[num2] = ("BlackHole", "WhiteHole", "坍缩");
		num2++;
		span[num2] = ("Overheat", "Adrenaline", "狂热");
		num2++;
		span[num2] = ("Bugle", "World", "天启");
		num2++;
		span[num2] = ("Cthulhu", "DuskDawn", "深渊觉醒");
		num2++;
		span[num2] = ("God", "Goddess", "神之共鸣");
		num2++;
		span[num2] = ("Parasite", "Plague", "生化危机");
		num2++;
		span[num2] = ("Drone", "Satellite", "天网");
		num2++;
		span[num2] = ("IceBeam", "Amber", "极寒地狱");
		num2++;
		span[num2] = ("Ragnarok", "NukeLeak", "末日审判");
		num2++;
		span[num2] = ("DeadHand", "DeagleKing", "致命一击");
		num2++;
		span[num2] = ("DragonSoul", "Dragonborn", "巨龙共鸣");
		num2++;
		span[num2] = ("MagneticPulse", "Thorns", "磁力荆棘");
		num2++;
		span[num2] = ("MagneticPulse", "RepulsionField", "禁区");
		_combos = list;
	}
}
