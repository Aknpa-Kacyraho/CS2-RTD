using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ReverseCausalityConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("delay_seconds")]
	public float DelaySeconds { get; set; } = 5f;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 1f;
}
