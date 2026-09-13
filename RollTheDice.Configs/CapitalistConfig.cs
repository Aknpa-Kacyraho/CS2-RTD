using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CapitalistConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("interval")]
	public float Interval { get; set; } = 5f;

	[JsonPropertyName("amount_per_tick")]
	public int AmountPerTick { get; set; } = 150;

	[JsonPropertyName("max_total")]
	public int MaxTotal { get; set; } = 3000;
}