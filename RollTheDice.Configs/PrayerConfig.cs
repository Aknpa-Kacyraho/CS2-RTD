using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PrayerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("pray_interval")]
	public float PrayInterval { get; set; } = 20f;

	[JsonPropertyName("success_chance")]
	public float SuccessChance { get; set; } = 0.5f;

	[JsonPropertyName("success_needed")]
	public int SuccessNeeded { get; set; } = 3;
}
