using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class OverheatConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("interval")]
	public float Interval { get; set; } = 5f;

	[JsonPropertyName("speed_per_stack")]
	public float SpeedPerStack { get; set; } = 0.05f;

	[JsonPropertyName("damage_per_stack")]
	public float DamagePerStack { get; set; } = 0.03f;

	[JsonPropertyName("max_stacks")]
	public int MaxStacks { get; set; } = 14;
}
