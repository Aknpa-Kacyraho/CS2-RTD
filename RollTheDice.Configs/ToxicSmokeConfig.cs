using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ToxicSmokeConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_per_second")]
	public int DamagePerSecond { get; set; } = 5;
}
