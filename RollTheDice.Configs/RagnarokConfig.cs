using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class RagnarokConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("invul_duration")]
	public float InvulDuration { get; set; } = 30f;

	[JsonPropertyName("round_duration")]
	public float RoundDuration { get; set; } = 60f;

	/// <summary>终焉降临时对所有敌人造成的伤害（取代原先献祭队友的自爆）。</summary>
	[JsonPropertyName("final_damage")]
	public int FinalDamage { get; set; } = 250;
}
