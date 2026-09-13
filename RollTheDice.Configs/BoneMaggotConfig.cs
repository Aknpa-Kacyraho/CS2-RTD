using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class BoneMaggotConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("mark_duration")]
	public float MarkDuration { get; set; } = 5f;

	[JsonPropertyName("reveal_through_walls")]
	public bool RevealThroughWalls { get; set; } = true;

	[JsonPropertyName("mark_damage_bonus")]
	public float MarkDamageBonus { get; set; } = 0.2f;

	[JsonPropertyName("kill_heal")]
	public int KillHeal { get; set; } = 25;
}
