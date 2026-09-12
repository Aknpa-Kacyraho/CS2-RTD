using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class AnatomistConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("headshot_bonus")]
	public float HeadshotBonus { get; set; } = 0.6f;

	[JsonPropertyName("body_penalty")]
	public float BodyPenalty { get; set; } = 0.2f;
}
