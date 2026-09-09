using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class MartyrdomConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("explosion_radius")]
	public float ExplosionRadius { get; set; } = 800f;

	[JsonPropertyName("explosion_damage")]
	public int ExplosionDamage { get; set; } = 200;
}
