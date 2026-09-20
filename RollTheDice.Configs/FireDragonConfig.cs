using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FireDragonConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("bonus_hp")]
	public int BonusHP { get; set; } = 300;

	[JsonPropertyName("bonus_armor")]
	public int BonusArmor { get; set; } = 400;

	[JsonPropertyName("burn_duration")]
	public float BurnDuration { get; set; } = 3f;

	[JsonPropertyName("burn_damage_per_sec")]
	public int BurnDamagePerSec { get; set; } = 30;
}
