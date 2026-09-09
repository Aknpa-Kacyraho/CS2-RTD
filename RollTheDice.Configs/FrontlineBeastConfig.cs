using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FrontlineBeastConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("speed_mult")]
	public float SpeedMult { get; set; } = 1.5f;

	[JsonPropertyName("speed_mult_per_kill")]
	public float SpeedMultPerKill { get; set; } = 0.5f;

	[JsonPropertyName("speed_mult_max")]
	public float SpeedMultMax { get; set; } = 2.5f;
}
