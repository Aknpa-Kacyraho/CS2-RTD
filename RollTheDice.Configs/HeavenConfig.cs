using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class HeavenConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("min_timescale")]
	public float MinTimescale { get; set; } = 0.5f;

	[JsonPropertyName("max_timescale")]
	public float MaxTimescale { get; set; } = 3f;

	[JsonPropertyName("step")]
	public float Step { get; set; } = 0.1f;

	[JsonPropertyName("step_interval")]
	public float StepInterval { get; set; } = 2f;
}
