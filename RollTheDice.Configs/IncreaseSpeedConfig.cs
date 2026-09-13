using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class IncreaseSpeedConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_bonus")]
	public float MaxBonus { get; set; } = 0.5f;

	[JsonPropertyName("gain_per_second")]
	public float GainPerSecond { get; set; } = 0.1f;
}
