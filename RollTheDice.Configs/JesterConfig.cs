using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class JesterConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_per_second")]
	public int DamagePerSecond { get; set; } = 1;

	[JsonPropertyName("heal_per_second")]
	public int HealPerSecond { get; set; } = 4;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 1.3f;
}
