using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class BugleConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 60f;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 2.0f;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 1.2f;
}
