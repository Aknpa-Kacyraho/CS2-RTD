using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PainConverterConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_pain")]
	public float MaxPain { get; set; } = 100f;

	[JsonPropertyName("decay_per_second")]
	public float DecayPerSecond { get; set; } = 3f;

	[JsonPropertyName("min_pain_to_activate")]
	public float MinPainToActivate { get; set; } = 40f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 20f;

	[JsonPropertyName("burst_duration")]
	public float BurstDuration { get; set; } = 5f;

	[JsonPropertyName("damage_per_pain")]
	public float DamagePerPain { get; set; } = 0.02f;

	[JsonPropertyName("damage_pain_cap")]
	public float DamagePainCap { get; set; } = 100f;

	[JsonPropertyName("speed_per_pain")]
	public float SpeedPerPain { get; set; } = 0.002f;

	[JsonPropertyName("burst_hp_per_second")]
	public float BurstHpPerSecond { get; set; } = 15f;
}
