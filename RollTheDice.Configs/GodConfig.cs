using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GodConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 40f;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 2f;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 1.5f;

	[JsonPropertyName("armor_bonus")]
	public int ArmorBonus { get; set; } = 200;
}