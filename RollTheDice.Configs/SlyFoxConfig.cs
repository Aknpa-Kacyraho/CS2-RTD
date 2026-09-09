using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SlyFoxConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("min_detonate")]
	public float MinDetonate { get; set; } = 0f;

	[JsonPropertyName("max_detonate")]
	public float MaxDetonate { get; set; } = 20f;
}
