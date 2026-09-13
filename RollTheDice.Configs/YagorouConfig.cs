using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class YagorouConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("kill_invuln_seconds")]
	public float KillInvulnSeconds { get; set; } = 1f;

	[JsonPropertyName("lethal_invuln_seconds")]
	public float LethalInvulnSeconds { get; set; } = 0.5f;

	[JsonPropertyName("lethal_saves_per_round")]
	public int LethalSavesPerRound { get; set; } = 1;
}
