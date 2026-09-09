using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PriestConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_amount")]
	public int HealAmount { get; set; } = 15;
}
