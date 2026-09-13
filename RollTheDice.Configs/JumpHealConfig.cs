using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class JumpHealConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_per_land")]
	public int HealPerLand { get; set; } = 10;
}