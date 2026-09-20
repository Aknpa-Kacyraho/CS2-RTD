#nullable enable
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 用 <c>ChangeSubclass</c> 把玩家手上的武器 / 雷换成工坊 addon（Aknpa_packs）里注册的自定义模型。
///
/// 条目名来自 addon 的 <c>scripts/weapons.vdata_c</c>（见 AGENTS §3）：<c>_base</c> 指向原武器 prefab，
/// 只改 <c>m_szWorldModel</c> 等。纯服务端配置，客户端无需装文件。
/// <b>必须在武器刚发放/创建时应用</b>（<c>GiveNamedItem</c> 后下一帧）；对已存在的老武器补切无效。
/// 注意：WeaponPaints 对刀也用 <c>ChangeSubclass</c>，二者会互相覆盖，时机冲突时以本插件为准。
/// </summary>
public static class WeaponSubclass
{
	// 自定义条目 → 需要先发放的基础武器实体名。
	private static readonly Dictionary<string, string> BaseOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		// 刀（都发 weapon_knife）
		["weapon_knife_flower"] = "weapon_knife",
		["weapon_knife_flower2"] = "weapon_knife",
		["weapon_knife_cyber_blue"] = "weapon_knife",
		["weapon_knife_cyber_pink"] = "weapon_knife",
		["weapon_knife_reimu_rod"] = "weapon_knife",
		["weapon_knife_gungair"] = "weapon_knife",
		["weapon_knife_flan_sword"] = "weapon_knife",
		["weapon_knife_youmu_katana"] = "weapon_knife",
		["weapon_knife_miko_yubi"] = "weapon_knife",
		["weapon_knife_candy"] = "weapon_knife",
		["weapon_knife_bacopa"] = "weapon_knife",
		["weapon_knife_tianxin"] = "weapon_knife",
		["weapon_knife_kasa"] = "weapon_knife",
		["weapon_knife_kagerou_claw"] = "weapon_knife",
		["weapon_knife_aya_fan"] = "weapon_knife",
		["weapon_knife_momiji_sword"] = "weapon_knife",
		["weapon_knife_juxueli_glass"] = "weapon_knife",
		["weapon_knife_clown_torch"] = "weapon_knife",
		["weapon_knife_daiyousei"] = "weapon_knife",
		["weapon_alicenote_sword"] = "weapon_knife",
		// 雷
		["weapon_plnade"] = "weapon_hegrenade",
		["weapon_magic_potion"] = "weapon_hegrenade",
		["weapon_sanae_signnade"] = "weapon_hegrenade",
		["weapon_plicegrenade"] = "weapon_smokegrenade",
		["weapon_plmolotov"] = "weapon_molotov",
		["weapon_kedama"] = "weapon_decoy",
		["weapon_knife_decoy"] = "weapon_decoy",
		["weapon_kedama_ice"] = "weapon_decoy",
		["weapon_kedama_steel"] = "weapon_decoy",
		["weapon_kedama_explode"] = "weapon_decoy",
		["weapon_ice_cream_1"] = "weapon_flashbang",
		["weapon_ice_cream_2"] = "weapon_flashbang",
		// 枪
		["weapon_lettyak"] = "weapon_ak47",
		["weapon_lilym4"] = "weapon_m4a1",
		["weapon_cirnofumo_p90"] = "weapon_p90",
		["weapon_daiyousei_mp7"] = "weapon_mp7",
	};

	/// <summary>条目对应的基础武器实体名（未收录返回 null）。</summary>
	public static string? BaseWeapon(string entry)
	{
		return BaseOf.TryGetValue(entry, out string? baseWeapon) ? baseWeapon : null;
	}

	/// <summary>发放 / 套用自定义武器模型（发放瞬间应用）。</summary>
	public static void Give(CCSPlayerController player, string entry)
	{
		if (player == null || !((CEntityInstance)player).IsValid || string.IsNullOrEmpty(entry))
		{
			return;
		}
		string? baseWeapon = BaseWeapon(entry);
		if (baseWeapon == null)
		{
			return;
		}
		bool isKnife = string.Equals(baseWeapon, "weapon_knife", StringComparison.OrdinalIgnoreCase);
		if (isKnife)
		{
			// 对"已存在的刀"改 subclass 只改世界模型/动作，手上 viewmodel 不刷新（见 AGENTS §3）。
			// 必须换一把全新的刀，再在发放瞬间换模，viewmodel 才会正确。
			CBasePlayerWeapon? old = FindWeapon(player, IsKnifeName);
			if (old != null && old.IsValid)
			{
				try
				{
					((CEntityInstance)old).Remove();
				}
				catch
				{
				}
			}
		}
		player.GiveNamedItem(baseWeapon);
		Server.NextWorldUpdate(delegate
		{
			Apply(player, entry, baseWeapon, isKnife);
		});
	}

	private static void Apply(CCSPlayerController player, string entry, string baseWeapon, bool isKnife)
	{
		try
		{
			if (player == null || !((CEntityInstance)player).IsValid)
			{
				return;
			}
			CBasePlayerWeapon? weapon = isKnife ? FindWeapon(player, IsKnifeName) : FindWeapon(player, (name) => string.Equals(name, baseWeapon, StringComparison.OrdinalIgnoreCase));
			if (weapon == null || !weapon.IsValid)
			{
				return;
			}
			((CEntityInstance)weapon).AcceptInput("ChangeSubclass", weapon, weapon, entry, 0);
		}
		catch (Exception ex)
		{
			try
			{
				RollTheDice.LogErr($"[WeaponSubclass] ChangeSubclass {entry} on {baseWeapon} failed: {ex.Message}\n");
			}
			catch
			{
			}
		}
	}

	private static bool IsKnifeName(string name)
	{
		return name.Contains("knife", StringComparison.OrdinalIgnoreCase);
	}

	private static CBasePlayerWeapon? FindWeapon(CCSPlayerController player, Func<string, bool> match)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return null;
		}
		var weapons = ((CBasePlayerPawn)pawn).WeaponServices?.MyWeapons;
		if (weapons == null)
		{
			return null;
		}
		foreach (CHandle<CBasePlayerWeapon> handle in weapons)
		{
			CBasePlayerWeapon? weapon = handle?.Value;
			if (weapon == null || !weapon.IsValid)
			{
				continue;
			}
			string? name = ((CEntityInstance)weapon).DesignerName;
			if (!string.IsNullOrEmpty(name) && match(name!))
			{
				return weapon;
			}
		}
		return null;
	}
}
