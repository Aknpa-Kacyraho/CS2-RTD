using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DivineDescentConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration_seconds")]
	public float DurationSeconds { get; set; } = 8f;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 90f;

	[JsonPropertyName("damage_bonus")]
	public float DamageBonus { get; set; } = 1f;

	[JsonPropertyName("speed_bonus")]
	public float SpeedBonus { get; set; } = 0.3f;

	[JsonPropertyName("heal_per_tick")]
	public int HealPerTick { get; set; } = 30;

	[JsonPropertyName("heal_interval")]
	public float HealInterval { get; set; } = 0.5f;

	[JsonPropertyName("max_health")]
	public int MaxHealth { get; set; } = 666;

	[JsonPropertyName("rune_radius")]
	public float RuneRadius { get; set; } = 160f;

	[JsonPropertyName("rune_outer_radius")]
	public float RuneOuterRadius { get; set; } = 200f;

	[JsonPropertyName("rune_segments")]
	public int RuneSegments { get; set; } = 24;

	[JsonPropertyName("rune_spokes")]
	public int RuneSpokes { get; set; } = 8;

	[JsonPropertyName("rune_width")]
	public float RuneWidth { get; set; } = 2f;

	[JsonPropertyName("rune_spin_speed")]
	public float RuneSpinSpeed { get; set; } = 1.6f;
}
