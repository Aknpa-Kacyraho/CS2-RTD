using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class VampireConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("lifesteal")]
	public float Lifesteal { get; set; } = 0.4f;

	[JsonPropertyName("low_hp_bonus")]
	public float LowHpBonus { get; set; } = 2f;
}