using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RarityConfig
{
	[JsonPropertyName("tier_weights")]
	public Dictionary<string, float> TierWeights { get; set; } = new Dictionary<string, float>
	{
		{ "common", 100f },
		{ "rare", 50f },
		{ "epic", 20f },
		{ "legendary", 5f },
		{ "combo", 1f },
	};

	[JsonPropertyName("dice_tier")]
	public Dictionary<string, string> DiceTier { get; set; } = new Dictionary<string, string>
	{
		{ "Fate", "legendary" },
		{ "God", "legendary" },
		{ "Ragnarok", "legendary" },
		{ "FourHorsemen", "legendary" },
		{ "Cthulhu", "legendary" },
		{ "DragonSoul", "legendary" },
		{ "FourtyTwo", "legendary" },
		{ "WheelOfFate", "epic" },
		{ "NukeLeak", "epic" },
		{ "Singularity", "epic" },
		{ "BlackHole", "epic" },
		{ "WhiteHole", "epic" },
		{ "GravityWell", "epic" },
		{ "Titanfall", "epic" },
		{ "Combo", "epic" },
		{ "PainConverter", "epic" },
		{ "DecoyDummy", "epic" },
		{ "Drone", "epic" },
		{ "Dragonborn", "epic" },
		{ "Void", "epic" },
		{ "ThunderChain", "epic" },
		{ "Reincarnation", "epic" },
		{ "Respawn", "epic" },
		{ "InfiniteProliferation", "epic" },
		{ "DivineResurrection", "epic" },
		{ "Necromancer", "epic" },
		{ "Emperor", "epic" },
		{ "Gaia", "epic" },
		{ "ResetOnReload", "rare" },
		{ "DamageMultiplier", "rare" },
		{ "IncreaseSpeed", "rare" },
		{ "Regeneration", "rare" },
		{ "Shield", "rare" },
		{ "HighGravity", "rare" },
		{ "DeagleKing", "rare" },
		{ "SniperElite", "rare" },
		{ "PistolMaster", "rare" },
		{ "GrenadeKing", "rare" },
		{ "LongerFlashes", "rare" },
		{ "Twilight", "rare" },
		{ "BoneMaggot", "rare" },
		{ "ReturnToSender", "rare" },
		{ "SwordSaint", "rare" },
		{ "Bank", "rare" },
		{ "Vampire", "rare" },
		{ "NoRecoil", "rare" },
		{ "GunGod", "rare" },
		{ "Hermit", "rare" },
		{ "Satellite", "rare" },
		{ "Countdown", "rare" },
		{ "GunHealer", "common" },
		{ "Fool", "common" },
		{ "NoExplosives", "common" },
		{ "Capitalist", "common" },
		{ "JumpHeal", "common" },
		{ "Mosquito", "common" },
		{ "Deaf", "common" },
		{ "Synced", "common" },
		{ "Payback", "common" },
		{ "BeyondHeaven", "combo" },
		{ "DeathKnightComplete", "combo" },
		{ "FireDragon", "combo" },
		{ "IceDragon", "combo" },
		{ "Phoenix", "combo" },
		{ "RadarStation", "combo" },
	};
}
