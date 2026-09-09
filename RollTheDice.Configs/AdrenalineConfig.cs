using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class AdrenalineConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp_threshold_percent")]
	public float HpThresholdPercent { get; set; } = 0.4f;

	[JsonPropertyName("speed_multiplier_min")]
	public float SpeedMultiplierMin { get; set; } = 1.4f;

	[JsonPropertyName("speed_multiplier_max")]
	public float SpeedMultiplierMax { get; set; } = 2f;

	[JsonPropertyName("reduction_min")]
	public float ReductionMin { get; set; } = 0.5f;

	[JsonPropertyName("reduction_max")]
	public float ReductionMax { get; set; } = 0.9f;
}
