using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RewindConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
