using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class IronHeadConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("headshot_reduction")]
	public float HeadshotReduction { get; set; } = 0.99f;
}
