using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CountdownConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("countdown")]
	public float Countdown { get; set; } = 60f;
}
