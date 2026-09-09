using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class EclipseConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("new_moon_duration")]
	public float NewMoonDuration { get; set; } = 15f;

	[JsonPropertyName("new_moon_speed")]
	public float NewMoonSpeed { get; set; } = 1.2f;

	[JsonPropertyName("new_moon_damage_mult")]
	public float NewMoonDamageMult { get; set; } = 0.8f;

	[JsonPropertyName("full_moon_duration")]
	public float FullMoonDuration { get; set; } = 15f;

	[JsonPropertyName("full_moon_speed")]
	public float FullMoonSpeed { get; set; } = 0.8f;

	[JsonPropertyName("full_moon_damage_mult")]
	public float FullMoonDamageMult { get; set; } = 1.5f;
}
