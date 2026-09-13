using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SyncedConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("range")]
	public float Range { get; set; } = 2000f;
}