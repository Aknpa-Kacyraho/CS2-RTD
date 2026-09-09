using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ForsakenConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
