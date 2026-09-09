using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DeagleKingConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
