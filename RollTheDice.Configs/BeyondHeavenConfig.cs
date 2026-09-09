using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class BeyondHeavenConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 9f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 60f;
}
