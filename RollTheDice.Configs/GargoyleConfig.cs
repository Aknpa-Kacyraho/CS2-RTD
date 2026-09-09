using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GargoyleConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 3f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 15f;
}
