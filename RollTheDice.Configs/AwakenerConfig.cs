using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class AwakenerConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>每次击杀/助攻获得的额外生命（会抬高生命上限，可突破原有上限）。</summary>
	[JsonPropertyName("hp_per_kill")]
	public int HpPerKill { get; set; } = 160;

	/// <summary>每次击杀/助攻获得的伤害加成（1 = +100%，可无限叠加）。</summary>
	[JsonPropertyName("damage_bonus_per_kill")]
	public float DamageBonusPerKill { get; set; } = 1f;

	/// <summary>每次击杀/助攻获得的减伤（0.4 = +40%）。</summary>
	[JsonPropertyName("reduction_per_kill")]
	public float ReductionPerKill { get; set; } = 0.4f;

	/// <summary>减伤叠加上限。</summary>
	[JsonPropertyName("reduction_cap")]
	public float ReductionCap { get; set; } = 0.99f;
}
