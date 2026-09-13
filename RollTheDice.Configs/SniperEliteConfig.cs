using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SniperEliteConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_bonus")]
	public float MaxBonus { get; set; } = 2f;

	[JsonPropertyName("charge_seconds")]
	public float ChargeSeconds { get; set; } = 3f;
}
