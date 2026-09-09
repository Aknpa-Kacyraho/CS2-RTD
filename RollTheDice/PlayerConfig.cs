using System.Text.Json.Serialization;

namespace RollTheDice;

public class PlayerConfig
{
	[JsonPropertyName("rtd_on_spawn")]
	public bool RtdOnSpawn { get; set; } = false;
}
