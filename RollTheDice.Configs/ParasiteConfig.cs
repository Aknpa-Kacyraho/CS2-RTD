using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ParasiteConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp_restore")]
	public int HpRestore { get; set; } = 50;

	[JsonPropertyName("speed_bonus")]
	public float SpeedBonus { get; set; } = 0.1f;

	[JsonPropertyName("damage_bonus")]
	public float DamageBonus { get; set; } = 0.1f;

	[JsonPropertyName("damage_cap")]
	public float DamageCap { get; set; } = 0.5f;
}
