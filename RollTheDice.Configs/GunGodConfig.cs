using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GunGodConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_reduction")]
	public float DamageReduction { get; set; } = 0.66f;
}
