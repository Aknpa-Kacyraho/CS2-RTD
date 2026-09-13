using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FoolConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("attack_whiff_chance")]
	public float AttackWhiffChance { get; set; } = 0.5f;

	[JsonPropertyName("invincibility_chance")]
	public float InvincibilityChance { get; set; } = 0.5f;

	[JsonPropertyName("invincibility_seconds")]
	public float InvincibilitySeconds { get; set; } = 2f;

	[JsonPropertyName("whiff_pity")]
	public int WhiffPity { get; set; } = 3;
}