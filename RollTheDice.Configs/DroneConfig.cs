using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DroneConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("orbit_radius")]
	public float OrbitRadius { get; set; } = 80f;

	[JsonPropertyName("orbit_height")]
	public float OrbitHeight { get; set; } = 60f;

	[JsonPropertyName("orbit_speed")]
	public float OrbitSpeed { get; set; } = 3f;

	[JsonPropertyName("attack_range")]
	public float AttackRange { get; set; } = 800f;

	[JsonPropertyName("fire_rate")]
	public float FireRate { get; set; } = 1.2f;

	[JsonPropertyName("damage_min")]
	public int DamageMin { get; set; } = 20;

	[JsonPropertyName("damage_max")]
	public int DamageMax { get; set; } = 35;
}
