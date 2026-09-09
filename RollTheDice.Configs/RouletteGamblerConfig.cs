using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RouletteGamblerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("death_chance")]
	public float DeathChance { get; set; } = 0.02f;

	[JsonPropertyName("bonus_max_percent")]
	public float BonusMaxPercent { get; set; } = 50f;
}
