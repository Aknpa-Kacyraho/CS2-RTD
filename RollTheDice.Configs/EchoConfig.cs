using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class EchoConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("delay_seconds")]
	public float DelaySeconds { get; set; } = 0.8f;

	[JsonPropertyName("damage_fraction")]
	public float DamageFraction { get; set; } = 0.5f;

	[JsonPropertyName("max_damage")]
	public float MaxDamage { get; set; } = 60f;
}
