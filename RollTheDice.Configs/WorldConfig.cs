using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class WorldConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("extra_dice_count")]
	public int ExtraDiceCount { get; set; } = 3;
}
