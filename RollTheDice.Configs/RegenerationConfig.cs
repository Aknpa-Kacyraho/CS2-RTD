using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RegenerationConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("out_of_combat_seconds")]
	public float OutOfCombatSeconds { get; set; } = 2f;

	[JsonPropertyName("heal_per_second")]
	public float HealPerSecond { get; set; } = 6f;
}
