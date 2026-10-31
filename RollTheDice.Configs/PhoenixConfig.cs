using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PhoenixConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("invul_duration")]
	public float InvulDuration { get; set; } = 10f;

	[JsonPropertyName("heal_hp")]
	public int HealHP { get; set; } = 444;

	[JsonPropertyName("heal_armor")]
	public int HealArmor { get; set; } = 444;

	[JsonPropertyName("explosion_radius")]
	public float ExplosionRadius { get; set; } = 500f;

	[JsonPropertyName("explosion_damage")]
	public int ExplosionDamage { get; set; } = 80;

	[JsonPropertyName("float_speed")]
	public float FloatSpeed { get; set; } = 30f;

	/// <summary>触发涅槃瞬间的全服音效（从每个真人身上发声）。</summary>
	[JsonPropertyName("trigger_sound")]
	public string TriggerSound { get; set; } = "sounds/touhou/extend.vsnd";

	[JsonPropertyName("trigger_sound_volume")]
	public float TriggerSoundVolume { get; set; } = 0.7f;

	/// <summary>涅槃结束爆炸的全服音效（soundevent 名如 c4.explode，或 .vsnd 路径）。</summary>
	[JsonPropertyName("finale_sound")]
	public string FinaleSound { get; set; } = "c4.explode";

	[JsonPropertyName("finale_sound_volume")]
	public float FinaleSoundVolume { get; set; } = 0.8f;
}
