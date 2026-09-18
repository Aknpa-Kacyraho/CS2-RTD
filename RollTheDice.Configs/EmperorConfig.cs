using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class EmperorConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("revive_invuln_seconds")]
	public float ReviveInvulnSeconds { get; set; } = 2f;

	/// <summary>每回合全队共享的复活次数（取代原先每个受害者各一次）。</summary>
	[JsonPropertyName("max_resurrections")]
	public int MaxResurrections { get; set; } = 4;
}