using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FibonacciConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
