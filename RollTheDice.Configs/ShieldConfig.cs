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

	[JsonPropertyName("absorb_damage")]
	public float AbsorbDamage { get; set; } = 150f;

	[JsonPropertyName("reduction")]
	public float Reduction { get; set; } = 0.5f;

	[JsonPropertyName("refresh_kills")]
	public int RefreshKills { get; set; } = 1;
}
