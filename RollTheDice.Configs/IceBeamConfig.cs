using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class IceBeamConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("freeze_chance")]
	public float FreezeChance { get; set; } = 0.2f;

	[JsonPropertyName("slow_amount")]
	public float SlowAmount { get; set; } = 0.5f;

	[JsonPropertyName("freeze_duration")]
	public float FreezeDuration { get; set; } = 1.5f;
}
