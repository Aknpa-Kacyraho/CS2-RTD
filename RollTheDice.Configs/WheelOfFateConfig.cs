using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class WheelOfFateConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>首次死亡的复活概率。</summary>
	[JsonPropertyName("revive_chance")]
	public float ReviveChance { get; set; } = 1f;

	/// <summary>每次复活后概率衰减值（与 Fate 同队时减半）。</summary>
	[JsonPropertyName("revive_decay")]
	public float ReviveDecay { get; set; } = 0.1f;

	/// <summary>复活概率下限（不会衰减到此值以下）。</summary>
	[JsonPropertyName("revive_chance_min")]
	public float ReviveChanceMin { get; set; } = 0.6f;

	/// <summary>每次复活时额外复活一名阵亡队友的概率（不随复活次数衰减）。</summary>
	[JsonPropertyName("teammate_revive_chance")]
	public float TeammateReviveChance { get; set; } = 0.5f;

	/// <summary>复活瞬间的全服音效（从每个真人身上发声）。</summary>
	[JsonPropertyName("revive_sound")]
	public string ReviveSound { get; set; } = "sounds/touhou/timeout.vsnd";

	[JsonPropertyName("revive_sound_volume")]
	public float ReviveSoundVolume { get; set; } = 0.7f;
}
