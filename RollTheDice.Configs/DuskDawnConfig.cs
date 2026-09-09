using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class DuskDawnConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("max_health")]
	public int MaxHealth { get; set; } = 150;

	[JsonPropertyName("heal_per_sec")]
	public int HealPerSec { get; set; } = 100;

	[JsonPropertyName("armor_per_sec")]
	public int ArmorPerSec { get; set; } = 10;
}
