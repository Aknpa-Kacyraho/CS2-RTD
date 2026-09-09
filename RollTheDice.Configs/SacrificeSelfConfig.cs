using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SacrificeSelfConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 1.2f;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 1.5f;
}
