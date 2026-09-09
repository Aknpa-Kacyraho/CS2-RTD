using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GunHealerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_per_shot")]
	public int HealPerShot { get; set; } = 5;
}
