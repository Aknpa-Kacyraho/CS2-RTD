using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SwapConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("cooldown")]
	public float Cooldown { get; set; } = 20f;

	[JsonPropertyName("max_distance")]
	public float MaxDistance { get; set; } = 900f;

	[JsonPropertyName("max_angle")]
	public float MaxAngle { get; set; } = 25f;
}
