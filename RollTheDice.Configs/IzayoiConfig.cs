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

	/// <summary>扰动下限（越小越慢，必须 &lt; 1）。</summary>
	[JsonPropertyName("min_factor")]
	public float MinFactor { get; set; } = 0.5f;

	/// <summary>扰动上限（仍须 &lt; 1，仅减速）。</summary>
	[JsonPropertyName("max_factor")]
	public float MaxFactor { get; set; } = 0.8f;
}
