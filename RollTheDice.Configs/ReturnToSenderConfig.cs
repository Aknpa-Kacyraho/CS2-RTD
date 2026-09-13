using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ReturnToSenderConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hits_required")]
	public int HitsRequired { get; set; } = 4;
}