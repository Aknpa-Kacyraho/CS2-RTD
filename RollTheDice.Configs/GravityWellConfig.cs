using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GravityWellConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("pull_radius")]
	public float PullRadius { get; set; } = 1500f;

	[JsonPropertyName("pull_strength")]
	public float PullStrength { get; set; } = 400f;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 10f;
}
