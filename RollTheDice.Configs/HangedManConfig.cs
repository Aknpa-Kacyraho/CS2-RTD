using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class HangedManConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("drain_hp")]
	public int DrainHp { get; set; } = 1;

	[JsonPropertyName("heal_hp")]
	public int HealHp { get; set; } = 4;
}
