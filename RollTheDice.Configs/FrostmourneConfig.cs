using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FrostmourneConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("knife_damage_reduction")]
	public float KnifeDamageReduction { get; set; } = 0.6f;

	[JsonPropertyName("heal_per_sec")]
	public int HealPerSec { get; set; } = 2;
}
