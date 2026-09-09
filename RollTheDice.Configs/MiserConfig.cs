using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class MiserConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("threshold")]
	public int Threshold { get; set; } = 785;

	[JsonPropertyName("reduction_per_step")]
	public float ReductionPerStep { get; set; } = 0.1f;

	[JsonPropertyName("max_reduction")]
	public float MaxReduction { get; set; } = 0.5f;
}
