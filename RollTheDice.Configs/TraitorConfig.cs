using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class TraitorConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("death_delay")]
	public float DeathDelay { get; set; } = 1.5f;
}
