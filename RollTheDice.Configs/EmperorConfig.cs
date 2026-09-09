using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class EmperorConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
