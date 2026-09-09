using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class InfiniteProliferationConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_respawns")]
	public int MaxRespawns { get; set; } = 4;

	[JsonPropertyName("base_armor")]
	public int BaseArmor { get; set; } = 100;
}
