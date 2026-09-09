using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ShieldConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("armor_min")]
	public int ArmorMin { get; set; } = 30;

	[JsonPropertyName("armor_max")]
	public int ArmorMax { get; set; } = 100;

	[JsonPropertyName("helmet")]
	public bool Helmet { get; set; } = true;
}
