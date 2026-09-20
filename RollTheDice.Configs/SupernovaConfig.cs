using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SupernovaConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration_seconds")]
	public float DurationSeconds { get; set; } = 5f;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 30f;

	[JsonPropertyName("range")]
	public float Range { get; set; } = 1200f;

	[JsonPropertyName("damage_per_second")]
	public float DamagePerSecond { get; set; } = 100f;

	[JsonPropertyName("hit_radius")]
	public float HitRadius { get; set; } = 70f;

	[JsonPropertyName("slow_percent")]
	public float SlowPercent { get; set; } = 0.5f;

	/// <summary>外层金色光束宽度（够粗才有"光柱"感）。</summary>
	[JsonPropertyName("beam_width_outer")]
	public float BeamWidthOuter { get; set; } = 20f;

	/// <summary>内层白色光束宽度。</summary>
	[JsonPropertyName("beam_width_inner")]
	public float BeamWidthInner { get; set; } = 9f;
}
