using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class TitanfallConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("lockdown_seconds")]
	public float LockdownSeconds { get; set; } = 60f;

	[JsonPropertyName("titan_hp")]
	public int TitanHP { get; set; } = 500;

	[JsonPropertyName("titan_armor")]
	public int TitanArmor { get; set; } = 500;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 1.5f;
}
