using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CurseConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("mark_seconds")]
	public float MarkSeconds { get; set; } = 8f;

	[JsonPropertyName("mark_damage_bonus")]
	public float MarkDamageBonus { get; set; } = 0.3f;

	[JsonPropertyName("team_damage_bonus")]
	public float TeamDamageBonus { get; set; } = 0.15f;

	[JsonPropertyName("team_bonus_seconds")]
	public float TeamBonusSeconds { get; set; } = 5f;
}
