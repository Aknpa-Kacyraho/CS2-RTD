using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GaiaConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("gain_per_second")]
	public float GainPerSecond { get; set; } = 2f;

	[JsonPropertyName("max_bonus")]
	public int MaxBonus { get; set; } = 200;
}