using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GaiaConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp_per_second")]
	public int HpPerSecond { get; set; } = 1;

	[JsonPropertyName("max_hp")]
	public int MaxHP { get; set; } = 500;
}
