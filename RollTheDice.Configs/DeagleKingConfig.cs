using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DeagleKingConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 3f;

	[JsonPropertyName("headshot_heal")]
	public int HeadshotHeal { get; set; } = 50;

	[JsonPropertyName("max_health_multiplier")]
	public float MaxHealthMultiplier { get; set; } = 1.5f;
}
