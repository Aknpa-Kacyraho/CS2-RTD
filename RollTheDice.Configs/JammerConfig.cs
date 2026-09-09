using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class JammerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 1.15f;
}
