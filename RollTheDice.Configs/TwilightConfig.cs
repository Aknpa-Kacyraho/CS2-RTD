using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class TwilightConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
