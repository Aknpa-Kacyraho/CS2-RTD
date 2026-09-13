using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class BankConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("interval")]
	public float Interval { get; set; } = 15f;

	[JsonPropertyName("teammates_count")]
	public int TeammatesCount { get; set; } = 2;

	[JsonPropertyName("amount")]
	public int Amount { get; set; } = 800;
}