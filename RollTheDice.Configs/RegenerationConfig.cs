using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RegenerationConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("heal_per_tick")]
	public int HealPerTick { get; set; } = 4;

	[JsonPropertyName("tick_interval")]
	public float TickInterval { get; set; } = 2f;
}
