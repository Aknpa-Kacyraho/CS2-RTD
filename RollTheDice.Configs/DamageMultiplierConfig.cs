using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DamageMultiplierConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("base_multiplier")]
	public float BaseMultiplier { get; set; } = 1f;

	[JsonPropertyName("max_multiplier")]
	public float MaxMultiplier { get; set; } = 2.2f;

	[JsonPropertyName("gain_per_second")]
	public float GainPerSecond { get; set; } = 0.2f;
}
