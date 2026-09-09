using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RoyalBarrierConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_health")]
	public int MaxHealth { get; set; } = 444;

	[JsonPropertyName("max_armor")]
	public int MaxArmor { get; set; } = 444;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 0.3f;
}
