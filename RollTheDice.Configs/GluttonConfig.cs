using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GluttonConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_extra_dice")]
	public int MaxExtraDice { get; set; } = 2;
}
