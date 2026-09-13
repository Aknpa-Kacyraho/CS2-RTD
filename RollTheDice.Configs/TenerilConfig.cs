using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class TenerilConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("slow_percent")]
	public float SlowPercent { get; set; } = 0.3f;

	[JsonPropertyName("slow_seconds")]
	public float SlowSeconds { get; set; } = 20f;

	[JsonPropertyName("health_multiplier")]
	public float HealthMultiplier { get; set; } = 0.5f;

	[JsonPropertyName("min_health")]
	public int MinHealth { get; set; } = 1;
}
