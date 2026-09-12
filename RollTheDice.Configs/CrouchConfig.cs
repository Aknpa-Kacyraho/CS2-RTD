using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CrouchConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_reduction")]
	public float DamageReduction { get; set; } = 0.30f;

	[JsonPropertyName("heal_per_second")]
	public float HealPerSecond { get; set; } = 5f;
}
