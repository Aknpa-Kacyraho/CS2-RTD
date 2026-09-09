using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RadarJammerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("blackout_duration")]
	public float BlackoutDuration { get; set; } = 20f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 25f;
}
