using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class C4ExpertConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;
}
