using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ProphetConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("gain_interval")]
	public float GainInterval { get; set; } = 5f;

	[JsonPropertyName("max_stacks")]
	public int MaxStacks { get; set; } = 10;
}
