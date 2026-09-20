using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SkyVerdictConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("range")]
	public float Range { get; set; } = 1200f;

	[JsonPropertyName("delay_seconds")]
	public float DelaySeconds { get; set; } = 10f;

	[JsonPropertyName("damage")]
	public int Damage { get; set; } = 350;

	[JsonPropertyName("radius")]
	public float Radius { get; set; } = 450f;

	[JsonPropertyName("falloff_radius")]
	public float FalloffRadius { get; set; } = 900f;

	[JsonPropertyName("min_damage_scale")]
	public float MinDamageScale { get; set; } = 0.4f;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 60f;

	[JsonPropertyName("rune_radius")]
	public float RuneRadius { get; set; } = 520f;

	[JsonPropertyName("rune_outer_radius")]
	public float RuneOuterRadius { get; set; } = 660f;

	[JsonPropertyName("rune_segments")]
	public int RuneSegments { get; set; } = 32;

	[JsonPropertyName("rune_spokes")]
	public int RuneSpokes { get; set; } = 12;

	[JsonPropertyName("rune_width")]
	public float RuneWidth { get; set; } = 2.4f;

	/// <summary>地面超位法阵的同心层数（外层带外环，内层逐层缩小）。</summary>
	[JsonPropertyName("sigil_rings")]
	public int SigilRings { get; set; } = 3;

	/// <summary>每往里一层半径乘 (1 - 该值)。</summary>
	[JsonPropertyName("sigil_shrink")]
	public float SigilShrink { get; set; } = 0.34f;

	/// <summary>法阵旋转速度倍率（层间正反交替、层越大越慢）。</summary>
	[JsonPropertyName("sigil_spin")]
	public float SigilSpin { get; set; } = 0.9f;

	/// <summary>倒数期间法阵额外的收缩比例：起手放大 (1+该值) 倍，命中瞬间合拢到 1 倍。</summary>
	[JsonPropertyName("sigil_contract")]
	public float SigilContract { get; set; } = 0.4f;

	// ---- 锁定点上方的竖向法阵塔（超位魔法）：够高、够大、够多（≥8 层）、够炫（双色交替） ----
	/// <summary>竖向叠加的法阵层数。</summary>
	[JsonPropertyName("tower_count")]
	public int TowerCount { get; set; } = 8;

	[JsonPropertyName("tower_height_base")]
	public float TowerHeightBase { get; set; } = 300f;

	[JsonPropertyName("tower_height_step")]
	public float TowerHeightStep { get; set; } = 260f;

	[JsonPropertyName("tower_radius")]
	public float TowerRadius { get; set; } = 620f;

	[JsonPropertyName("tower_outer_radius")]
	public float TowerOuterRadius { get; set; } = 760f;

	[JsonPropertyName("tower_radius_decay")]
	public float TowerRadiusDecay { get; set; }

	[JsonPropertyName("tower_segments")]
	public int TowerSegments { get; set; } = 20;

	[JsonPropertyName("tower_spokes")]
	public int TowerSpokes { get; set; } = 6;

	[JsonPropertyName("tower_spin")]
	public float TowerSpin { get; set; } = 0.5f;

	[JsonPropertyName("tower_contract")]
	public float TowerContract { get; set; } = 0.15f;

	[JsonPropertyName("pillar_height")]
	public float PillarHeight { get; set; } = 2500f;

	[JsonPropertyName("pillar_width")]
	public float PillarWidth { get; set; } = 28f;

	[JsonPropertyName("pillar_life")]
	public float PillarLife { get; set; } = 1.5f;

	[JsonPropertyName("launch_force")]
	public float LaunchForce { get; set; } = 260f;

	[JsonPropertyName("whiteout_seconds")]
	public float WhiteoutSeconds { get; set; } = 1.2f;

	[JsonPropertyName("shake_amplitude")]
	public float ShakeAmplitude { get; set; } = 16f;

	[JsonPropertyName("shake_frequency")]
	public float ShakeFrequency { get; set; } = 80f;

	[JsonPropertyName("shake_duration")]
	public float ShakeDuration { get; set; } = 1.2f;

	[JsonPropertyName("shake_radius")]
	public float ShakeRadius { get; set; } = 4000f;

	/// <summary>命中瞬间全服广播的爆炸音效（soundevent 名或 .vsnd 路径）。默认 C4 爆炸。</summary>
	[JsonPropertyName("explosion_sound")]
	public string ExplosionSound { get; set; } = "c4.explode";

	[JsonPropertyName("explosion_sound_volume")]
	public float ExplosionSoundVolume { get; set; } = 1f;
}
