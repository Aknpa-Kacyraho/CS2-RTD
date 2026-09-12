using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PainConverterConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_pain")]
	public float MaxPain { get; set; } = 250f;

	[JsonPropertyName("decay_per_second")]
	public float DecayPerSecond { get; set; } = 4f;

	[JsonPropertyName("min_pain_to_activate")]
	public float MinPainToActivate { get; set; } = 80f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 20f;

	[JsonPropertyName("burst_duration")]
	public float BurstDuration { get; set; } = 5f;

	[JsonPropertyName("damage_per_pain")]
	public float DamagePerPain { get; set; } = 0.01f;

	[JsonPropertyName("damage_pain_cap")]
	public float DamagePainCap { get; set; } = 200f;

	[JsonPropertyName("speed_per_pain")]
	public float SpeedPerPain { get; set; } = 0.001f;

	[JsonPropertyName("burst_hp_per_second")]
	public float BurstHpPerSecond { get; set; } = 15f;
}
