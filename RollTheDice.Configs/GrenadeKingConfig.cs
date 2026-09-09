using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GrenadeKingConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("multiplier_min")]
	public float MultiplierMin { get; set; } = 3f;

	[JsonPropertyName("multiplier_max")]
	public float MultiplierMax { get; set; } = 6f;
}
