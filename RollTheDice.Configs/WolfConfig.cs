using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class WolfConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp_per_wolf")]
	public int HpPerWolf { get; set; } = 30;

	[JsonPropertyName("armor_per_wolf")]
	public int ArmorPerWolf { get; set; } = 30;

	[JsonPropertyName("damage_per_wolf")]
	public float DamagePerWolf { get; set; } = 0.1f;

	[JsonPropertyName("speed_per_wolf")]
	public float SpeedPerWolf { get; set; } = 0.1f;

	[JsonPropertyName("bonus_delay")]
	public float BonusDelay { get; set; } = 5f;
}
