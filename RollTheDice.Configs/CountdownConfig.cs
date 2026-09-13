using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CountdownConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("seconds")]
	public float Seconds { get; set; } = 5f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 60f;
}