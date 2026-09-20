using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class FinalJudgmentConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 90f;

	[JsonPropertyName("delay_seconds")]
	public float DelaySeconds { get; set; } = 20f;

	// ---- 空中法阵塔（超位魔法）：够高（不被地形遮挡）、够大（远处看清）、够多（≥8 层往上）、够炫（双色交替） ----
	/// <summary>竖向叠加的法阵层数（超位魔法一般 ≥8）。</summary>
	[JsonPropertyName("tower_count")]
	public int TowerCount { get; set; } = 8;

	/// <summary>最底层法阵离施法者脚下的高度。</summary>
	[JsonPropertyName("tower_height_base")]
	public float TowerHeightBase { get; set; } = 350f;

	/// <summary>每高一层额外抬升的高度。</summary>
	[JsonPropertyName("tower_height_step")]
	public float TowerHeightStep { get; set; } = 280f;

	/// <summary>最底层法阵的半径（够大）。</summary>
	[JsonPropertyName("tower_radius")]
	public float TowerRadius { get; set; } = 700f;

	[JsonPropertyName("tower_outer_radius")]
	public float TowerOuterRadius { get; set; } = 850f;

	/// <summary>每高一层半径乘 (1 - 该值)，0 = 圆柱等大。</summary>
	[JsonPropertyName("tower_radius_decay")]
	public float TowerRadiusDecay { get; set; }

	[JsonPropertyName("tower_segments")]
	public int TowerSegments { get; set; } = 20;

	[JsonPropertyName("tower_spokes")]
	public int TowerSpokes { get; set; } = 6;

	/// <summary>法阵塔旋转速度倍率（层间正反交替）。</summary>
	[JsonPropertyName("tower_spin")]
	public float TowerSpin { get; set; } = 0.5f;

	/// <summary>倒数期间法阵塔额外的收缩比例（起手放大、引爆合拢）。</summary>
	[JsonPropertyName("tower_contract")]
	public float TowerContract { get; set; } = 0.15f;

	[JsonPropertyName("ground_radius")]
	public float GroundRadius { get; set; } = 620f;

	[JsonPropertyName("ground_outer_radius")]
	public float GroundOuterRadius { get; set; } = 820f;

	[JsonPropertyName("rune_segments")]
	public int RuneSegments { get; set; } = 28;

	[JsonPropertyName("rune_spokes")]
	public int RuneSpokes { get; set; } = 10;

	[JsonPropertyName("rune_width")]
	public float RuneWidth { get; set; } = 2.4f;

	/// <summary>地面超位法阵的同心层数。</summary>
	[JsonPropertyName("sigil_rings")]
	public int SigilRings { get; set; } = 3;

	/// <summary>每往里一层半径乘 (1 - 该值)。</summary>
	[JsonPropertyName("sigil_shrink")]
	public float SigilShrink { get; set; } = 0.36f;

	/// <summary>地面法阵旋转速度倍率。</summary>
	[JsonPropertyName("sigil_spin")]
	public float SigilSpin { get; set; } = 0.8f;

	/// <summary>倒数期间地面法阵额外的收缩比例：起手放大 (1+该值) 倍，引爆瞬间合拢到 1 倍。</summary>
	[JsonPropertyName("sigil_contract")]
	public float SigilContract { get; set; } = 0.35f;

	[JsonPropertyName("max_damage")]
	public int MaxDamage { get; set; } = 2000;

	[JsonPropertyName("min_damage")]
	public int MinDamage { get; set; } = 250;

	[JsonPropertyName("falloff_radius")]
	public float FalloffRadius { get; set; } = 8000f;

	[JsonPropertyName("pillar_height")]
	public float PillarHeight { get; set; } = 3000f;

	[JsonPropertyName("pillar_width")]
	public float PillarWidth { get; set; } = 40f;

	[JsonPropertyName("pillar_life")]
	public float PillarLife { get; set; } = 2.5f;

	[JsonPropertyName("whiteout_seconds")]
	public float WhiteoutSeconds { get; set; } = 2.5f;

	[JsonPropertyName("shake_amplitude")]
	public float ShakeAmplitude { get; set; } = 24f;

	[JsonPropertyName("shake_frequency")]
	public float ShakeFrequency { get; set; } = 100f;

	[JsonPropertyName("shake_duration")]
	public float ShakeDuration { get; set; } = 2f;

	[JsonPropertyName("shake_radius")]
	public float ShakeRadius { get; set; } = 6000f;

	/// <summary>引爆瞬间全服广播的爆炸音效（soundevent 名或 .vsnd 路径）。默认 C4 爆炸。</summary>
	[JsonPropertyName("explosion_sound")]
	public string ExplosionSound { get; set; } = "c4.explode";

	[JsonPropertyName("explosion_sound_volume")]
	public float ExplosionSoundVolume { get; set; } = 1f;
}
