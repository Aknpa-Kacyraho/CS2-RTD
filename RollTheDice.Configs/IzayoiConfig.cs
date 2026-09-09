using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class IzayoiConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration_seconds")]
	public float DurationSeconds { get; set; } = 5f;

	[JsonPropertyName("interval_seconds")]
	public float IntervalSeconds { get; set; } = 10f;
}
