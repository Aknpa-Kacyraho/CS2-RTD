using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SwordSaintConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("block_angle_degrees")]
	public float BlockAngleDegrees { get; set; } = 60f;
}