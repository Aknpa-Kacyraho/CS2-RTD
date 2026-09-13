using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GunHealerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_per_hit")]
	public int HealPerHit { get; set; } = 5;
}
