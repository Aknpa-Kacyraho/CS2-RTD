using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class JumpHealConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_min")]
	public int HealMin { get; set; } = 10;

	[JsonPropertyName("heal_max")]
	public int HealMax { get; set; } = 15;
}
