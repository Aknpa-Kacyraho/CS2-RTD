using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class HermitConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("reveal_seconds")]
	public float RevealSeconds { get; set; } = 2f;
}