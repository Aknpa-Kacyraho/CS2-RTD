using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SoulEaterConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_min")]
	public int HealMin { get; set; } = 30;

	[JsonPropertyName("heal_max")]
	public int HealMax { get; set; } = 50;
}
