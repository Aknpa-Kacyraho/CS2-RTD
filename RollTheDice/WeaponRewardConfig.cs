using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice;

/// <summary>
/// 传说 / combo dice 抽到时附赠的换模武器（走 <c>ChangeSubclass</c>，见 <see cref="RollTheDice.Utils.WeaponSubclass"/>）。
/// <c>weapons</c> 里有映射的用贴合主题的武器；没有的从 <c>random_knives</c> / <c>random_grenades</c> 里随机一件。
/// </summary>
public class WeaponRewardConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>dice 类名 → vdata 条目（贴合主题）。</summary>
	[JsonPropertyName("weapons")]
	public Dictionary<string, string> Weapons { get; set; } = new Dictionary<string, string>
	{
		{ "SwordSaint", "weapon_knife_youmu_katana" },
		{ "Ragnarok", "weapon_knife_gungair" },
		{ "WolfKing", "weapon_knife_kagerou_claw" },
		{ "God", "weapon_knife_miko_yubi" },
		{ "DivineDescent", "weapon_knife_reimu_rod" },
		{ "SkyVerdict", "weapon_sanae_signnade" },
		{ "FinalJudgment", "weapon_magic_potion" },
	};

	[JsonPropertyName("random_knives")]
	public List<string> RandomKnives { get; set; } = new List<string>
	{
		"weapon_knife_flower",
		"weapon_knife_flower2",
		"weapon_knife_cyber_blue",
		"weapon_knife_cyber_pink",
		"weapon_knife_reimu_rod",
		"weapon_knife_gungair",
		"weapon_knife_flan_sword",
		"weapon_knife_youmu_katana",
		"weapon_knife_miko_yubi",
		"weapon_knife_candy",
		"weapon_knife_bacopa",
		"weapon_knife_tianxin",
		"weapon_knife_kasa",
		"weapon_knife_kagerou_claw",
		"weapon_knife_aya_fan",
		"weapon_knife_momiji_sword",
		"weapon_knife_juxueli_glass",
		"weapon_knife_clown_torch",
		"weapon_knife_daiyousei",
		"weapon_alicenote_sword",
	};

	[JsonPropertyName("random_grenades")]
	public List<string> RandomGrenades { get; set; } = new List<string>
	{
		"weapon_plnade",
		"weapon_plicegrenade",
		"weapon_plmolotov",
		"weapon_kedama",
		"weapon_knife_decoy",
		"weapon_magic_potion",
		"weapon_sanae_signnade",
		"weapon_kedama_ice",
		"weapon_kedama_steel",
		"weapon_kedama_explode",
		"weapon_ice_cream_1",
		"weapon_ice_cream_2",
	};
}
