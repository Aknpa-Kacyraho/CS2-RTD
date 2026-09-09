using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CapitalistConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("money_per_death")]
	public int MoneyPerDeath { get; set; } = 500;
}
