using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DivineResurrectionConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("chance")]
	public float Chance { get; set; } = 0.5f;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 30f;
}
