using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FourtyTwoConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("interval")]
	public float Interval { get; set; } = 22f;

	[JsonPropertyName("invul_duration")]
	public float InvulDuration { get; set; } = 6f;

	[JsonPropertyName("invis_duration")]
	public float InvisDuration { get; set; } = 2f;
}
