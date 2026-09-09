using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SacrificeConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("required_teammate_deaths")]
	public int RequiredTeammateDeaths { get; set; } = 4;

	[JsonPropertyName("revive_hp")]
	public int ReviveHP { get; set; } = 300;

	[JsonPropertyName("revive_armor")]
	public int ReviveArmor { get; set; } = 300;

	[JsonPropertyName("speed_multiplier")]
	public float SpeedMultiplier { get; set; } = 2f;
}
