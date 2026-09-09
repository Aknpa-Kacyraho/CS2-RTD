using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SkylineConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("flight_duration")]
	public float FlightDuration { get; set; } = 3f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 30f;
}
