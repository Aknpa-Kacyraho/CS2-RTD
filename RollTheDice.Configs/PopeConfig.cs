using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PopeConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("health_multiplier")]
	public int HealthMultiplier { get; set; } = 2;
}
