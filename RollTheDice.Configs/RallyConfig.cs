using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RallyConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("radius")]
	public float Radius { get; set; } = 500f;

	[JsonPropertyName("damage_per_ally")]
	public float DamagePerAlly { get; set; } = 0.05f;

	[JsonPropertyName("reduction_per_ally")]
	public float ReductionPerAlly { get; set; } = 0.02f;

	[JsonPropertyName("max_stacks")]
	public int MaxStacks { get; set; } = 5;

	[JsonPropertyName("refresh_interval")]
	public float RefreshInterval { get; set; } = 0.25f;
}
