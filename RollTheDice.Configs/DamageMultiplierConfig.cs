using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DamageMultiplierConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("min_multiplier")]
	public float MinMultiplier { get; set; } = 1.4f;

	[JsonPropertyName("max_multiplier")]
	public float MaxMultiplier { get; set; } = 2.2f;
}
