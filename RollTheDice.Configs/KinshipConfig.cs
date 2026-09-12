using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class KinshipConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("invuln_seconds")]
	public float InvulnSeconds { get; set; } = 1f;
}
