using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FireLordConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("fire_heal_per_sec")]
	public int FireHealPerSec { get; set; } = 15;

	[JsonPropertyName("fire_damage_bonus")]
	public float FireDamageBonus { get; set; } = 0.3f;

	[JsonPropertyName("molotov_interval")]
	public float MolotovInterval { get; set; } = 20f;
}
