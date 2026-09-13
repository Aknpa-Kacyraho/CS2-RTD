using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GodConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 90f;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 3f;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 3f;

	[JsonPropertyName("armor_bonus")]
	public int ArmorBonus { get; set; } = 500;

	[JsonPropertyName("damage_reduction")]
	public float DamageReduction { get; set; } = 0.6f;

	[JsonPropertyName("heal_on_kill")]
	public int HealOnKill { get; set; } = 150;

	[JsonPropertyName("invuln_seconds")]
	public float InvulnSeconds { get; set; } = 1.5f;
}
