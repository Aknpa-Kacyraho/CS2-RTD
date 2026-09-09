using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FourHorsemenConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("war_damage_bonus")]
	public float WarDamageBonus { get; set; } = 0.5f;

	[JsonPropertyName("war_damage_taken")]
	public float WarDamageTaken { get; set; } = 0.5f;

	[JsonPropertyName("plague_damage_per_second")]
	public int PlagueDamagePerSecond { get; set; } = 2;

	[JsonPropertyName("death_killer_damage")]
	public int DeathKillerDamage { get; set; } = 100;
}
