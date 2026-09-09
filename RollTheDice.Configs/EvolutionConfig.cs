using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class EvolutionConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("evolve_interval")]
	public float EvolveInterval { get; set; } = 25f;

	[JsonPropertyName("max_stacks")]
	public int MaxStacks { get; set; } = 5;

	[JsonPropertyName("damage_per_stack")]
	public float DamagePerStack { get; set; } = 0.2f;

	[JsonPropertyName("speed_per_stack")]
	public float SpeedPerStack { get; set; } = 0.2f;

	[JsonPropertyName("hp_per_stack")]
	public int HpPerStack { get; set; } = 30;
}
