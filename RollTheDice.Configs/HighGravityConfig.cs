using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class HighGravityConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("gravity_scale")]
	public float GravityScale { get; set; } = 4f;

	[JsonPropertyName("damage_reduction")]
	public float DamageReduction { get; set; } = 0.4f;

	[JsonPropertyName("min_fall_speed")]
	public float MinFallSpeed { get; set; } = 500f;

	[JsonPropertyName("shock_damage")]
	public int ShockDamage { get; set; } = 40;

	[JsonPropertyName("shock_radius")]
	public float ShockRadius { get; set; } = 250f;

	[JsonPropertyName("shock_slow")]
	public float ShockSlow { get; set; } = 0.5f;

	[JsonPropertyName("shock_slow_seconds")]
	public float ShockSlowSeconds { get; set; } = 2f;
}
