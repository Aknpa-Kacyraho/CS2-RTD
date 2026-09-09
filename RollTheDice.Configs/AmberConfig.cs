using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class AmberConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("freeze_chance_min")]
	public float FreezeChanceMin { get; set; } = 0.4f;

	[JsonPropertyName("freeze_chance_max")]
	public float FreezeChanceMax { get; set; } = 0.7f;

	[JsonPropertyName("freeze_duration")]
	public float FreezeDuration { get; set; } = 2f;
}
