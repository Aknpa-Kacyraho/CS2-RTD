using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DragonSoulConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("bonus_hp")]
	public int BonusHP { get; set; } = 150;

	[JsonPropertyName("teammate_draw_chance")]
	public float TeammateDrawChance { get; set; } = 0.1f;
}
