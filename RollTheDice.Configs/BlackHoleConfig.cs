using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class BlackHoleConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("pull_radius")]
	public float PullRadius { get; set; } = 300f;

	[JsonPropertyName("pull_strength")]
	public float PullStrength { get; set; } = 150f;
}
