using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GrenadeKingConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 2.5f;

	[JsonPropertyName("radius_multiplier")]
	public float RadiusMultiplier { get; set; } = 1.25f;
}
