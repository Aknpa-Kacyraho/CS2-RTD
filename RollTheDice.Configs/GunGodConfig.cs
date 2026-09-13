using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GunGodConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("per_kill")]
	public float PerKill { get; set; } = 0.3f;

	[JsonPropertyName("max_reduction")]
	public float MaxReduction { get; set; } = 0.66f;
}