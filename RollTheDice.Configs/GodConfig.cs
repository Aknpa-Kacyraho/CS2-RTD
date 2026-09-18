using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GodConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 2f;

	[JsonPropertyName("damage_reduction")]
	public float DamageReduction { get; set; } = 0.2f;

	[JsonPropertyName("max_health")]
	public int MaxHealth { get; set; } = 666;

	[JsonPropertyName("armor_value")]
	public int ArmorValue { get; set; } = 666;

	[JsonPropertyName("invuln_seconds")]
	public float InvulnSeconds { get; set; } = 1.5f;

	/// <summary>击杀敌人回复的生命（与 Goddess 组合翻倍）。</summary>
	[JsonPropertyName("heal_on_kill")]
	public int HealOnKill { get; set; } = 50;
}
