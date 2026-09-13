using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SatelliteConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("gravity")]
	public float Gravity { get; set; } = 0.08f;

	[JsonPropertyName("air_speed_bonus")]
	public float AirSpeedBonus { get; set; } = 0.3f;
}