using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SmokeBombConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp_threshold_percent")]
	public float HpThresholdPercent { get; set; } = 0.25f;

	[JsonPropertyName("invisibility_seconds")]
	public float InvisibilitySeconds { get; set; } = 3f;
}
