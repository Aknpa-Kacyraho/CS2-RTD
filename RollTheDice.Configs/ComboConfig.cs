using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ComboConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("window_seconds")]
	public float WindowSeconds { get; set; } = 3f;

	[JsonPropertyName("damage_per_stack")]
	public float DamagePerStack { get; set; } = 0.10f;

	[JsonPropertyName("max_stacks")]
	public int MaxStacks { get; set; } = 10;

	[JsonPropertyName("heal_threshold")]
	public int HealThreshold { get; set; } = 5;

	[JsonPropertyName("heal_per_hit")]
	public int HealPerHit { get; set; } = 5;

	[JsonPropertyName("full_stack_speed")]
	public float FullStackSpeed { get; set; } = 0.15f;

	[JsonPropertyName("glow_seconds")]
	public float GlowSeconds { get; set; } = 5f;

	[JsonPropertyName("min_hit_interval")]
	public float MinHitInterval { get; set; } = 0.1f;
}
