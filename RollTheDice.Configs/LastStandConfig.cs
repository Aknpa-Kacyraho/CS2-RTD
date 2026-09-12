using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class LastStandConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 3f;

	[JsonPropertyName("clip_threshold")]
	public int ClipThreshold { get; set; } = 0;

	[JsonPropertyName("window_seconds")]
	public float WindowSeconds { get; set; } = 0.2f;
}
