using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ProphetConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
