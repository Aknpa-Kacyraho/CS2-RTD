using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PaybackConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_min")]
	public int DamageMin { get; set; } = 30;

	[JsonPropertyName("damage_max")]
	public int DamageMax { get; set; } = 80;
}
