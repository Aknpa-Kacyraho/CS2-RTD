using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class WolfKingConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp")]
	public int HP { get; set; } = 300;

	[JsonPropertyName("damage_bonus")]
	public float DamageBonus { get; set; } = 0.4f;

	[JsonPropertyName("speed_bonus")]
	public float SpeedBonus { get; set; } = 0.4f;
}
