using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class MagneticPulseConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 12f;

	[JsonPropertyName("damage_threshold")]
	public float DamageThreshold { get; set; } = 20f;

	[JsonPropertyName("damage_window")]
	public float DamageWindow { get; set; } = 1f;

	[JsonPropertyName("radius")]
	public float Radius { get; set; } = 500f;

	[JsonPropertyName("slow_amount")]
	public float SlowAmount { get; set; } = 0.4f;

	[JsonPropertyName("slow_duration")]
	public float SlowDuration { get; set; } = 3f;
}
