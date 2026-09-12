using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ReloadGapConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("reload_damage_reduction")]
	public float ReloadDamageReduction { get; set; } = 0.80f;

	[JsonPropertyName("post_reload_damage_bonus")]
	public float PostReloadDamageBonus { get; set; } = 0.40f;

	[JsonPropertyName("post_reload_duration")]
	public float PostReloadDuration { get; set; } = 3f;
}
