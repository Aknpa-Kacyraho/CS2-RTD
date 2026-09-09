using System.Text.Json.Serialization;

namespace RollTheDice;

public class MapConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("dices")]
	public DicesConfig Dices { get; set; } = new DicesConfig();
}
