using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class UniverseConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_restores")]
	public int MaxRestores { get; set; } = 2;
}
