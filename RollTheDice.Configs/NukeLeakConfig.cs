using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class NukeLeakConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("detonation_seconds")]
	public float DetonationSeconds { get; set; } = 60f;
}
