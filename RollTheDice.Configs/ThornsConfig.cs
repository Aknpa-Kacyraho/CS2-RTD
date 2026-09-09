using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ThornsConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("reflect_damage_min")]
	public int ReflectDamageMin { get; set; } = 5;

	[JsonPropertyName("reflect_damage_max")]
	public int ReflectDamageMax { get; set; } = 25;
}
