using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class VoidConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 5f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 45f;
}
