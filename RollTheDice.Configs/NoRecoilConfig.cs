using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class NoRecoilConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("charge_seconds")]
	public float ChargeSeconds { get; set; } = 0.1f;

	[JsonPropertyName("damage_bonus")]
	public float DamageBonus { get; set; } = 0.1f;
}