using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

/// <summary>
/// 「坠落天空」（内部名 FinalJudgment）配置。
/// 两阶段：先以落点为圆心长出苍白色立体穹顶，再在高空铺开蓝白法阵群、贯下通天光柱。
/// </summary>
public class FinalJudgmentConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 90f;

	/// <summary>按 E 到引爆的倒数时长。</summary>
	[JsonPropertyName("delay_seconds")]
	public float DelaySeconds { get; set; } = 20f;

	// ---- 阶段一：苍白色立体穹顶（施法者身边的构筑体） ----

	[JsonPropertyName("dome_enabled")]
	public bool DomeEnabled { get; set; } = true;

	/// <summary>穹顶半径（≈10m）。</summary>
	[JsonPropertyName("dome_radius")]
	public float DomeRadius { get; set; } = 480f;

	/// <summary>纬线圈数（自下而上分批生长）。</summary>
	[JsonPropertyName("dome_latitude_rings")]
	public int DomeLatitudeRings { get; set; } = 8;

	/// <summary>每圈纬线的段数。</summary>
	[JsonPropertyName("dome_segments")]
	public int DomeSegments { get; set; } = 48;

	/// <summary>经线条数。</summary>
	[JsonPropertyName("dome_meridians")]
	public int DomeMeridians { get; set; } = 12;

	/// <summary>穹顶从地面长到顶点所用的秒数。</summary>
	[JsonPropertyName("dome_grow_seconds")]
	public float DomeGrowSeconds { get; set; } = 2.4f;

	[JsonPropertyName("dome_spin")]
	public float DomeSpin { get; set; } = 0.35f;

	/// <summary>倒数末尾亮度脉冲持续的秒数（发动信号）。</summary>
	[JsonPropertyName("dome_pulse_seconds")]
	public float DomePulseSeconds { get; set; } = 3f;

	// ---- 阶段二：头顶天空法阵群 ----

	[JsonPropertyName("sky_enabled")]
	public bool SkyEnabled { get; set; } = true;

	/// <summary>法阵层数（"数十个"）。</summary>
	[JsonPropertyName("sky_count")]
	public int SkyCount { get; set; } = 16;

	[JsonPropertyName("sky_height_start")]
	public float SkyHeightStart { get; set; } = 900f;

	[JsonPropertyName("sky_height_end")]
	public float SkyHeightEnd { get; set; } = 3900f;

	[JsonPropertyName("sky_radius_start")]
	public float SkyRadiusStart { get; set; } = 1200f;

	/// <summary>最高层的半径（越小越显嵌套）。</summary>
	[JsonPropertyName("sky_radius_end")]
	public float SkyRadiusEnd { get; set; } = 320f;

	/// <summary>奇数层半径 = 相邻偶数层 × 该值（0.2~1）。制造"大小明显不一"，别调太小。</summary>
	[JsonPropertyName("sky_radius_alternate")]
	public float SkyRadiusAlternate { get; set; } = 0.55f;

	/// <summary>按 E 后延迟多久开始升空铺阵。</summary>
	[JsonPropertyName("sky_start_delay")]
	public float SkyStartDelay { get; set; } = 3f;

	/// <summary>相邻两层的出现间隔。</summary>
	[JsonPropertyName("sky_layer_delay")]
	public float SkyLayerDelay { get; set; } = 0.45f;

	[JsonPropertyName("sky_grow_seconds")]
	public float SkyGrowSeconds { get; set; } = 0.5f;

	[JsonPropertyName("sky_spin")]
	public float SkySpin { get; set; } = 0.5f;

	/// <summary>倒数末尾整体向内合拢的比例（0.22 = 收 22%）。</summary>
	[JsonPropertyName("sky_contract")]
	public float SkyContract { get; set; } = 0.22f;

	/// <summary>合拢在引爆前多少秒内完成。</summary>
	[JsonPropertyName("sky_contract_seconds")]
	public float SkyContractSeconds { get; set; } = 3f;

	// ---- 通天光柱 / 地面冲击环 ----

	/// <summary>光柱从地面向上延伸的高度（要盖过最高层法阵）。</summary>
	[JsonPropertyName("pillar_height")]
	public float PillarHeight { get; set; } = 4200f;

	/// <summary>光柱半径（直径 = 2×，默认 2600u ≈ 50m）。</summary>
	[JsonPropertyName("pillar_radius")]
	public float PillarRadius { get; set; } = 1300f;

	[JsonPropertyName("pillar_life")]
	public float PillarLife { get; set; } = 2.5f;

	[JsonPropertyName("shock_rings")]
	public int ShockRings { get; set; } = 3;

	[JsonPropertyName("shock_radius")]
	public float ShockRadius { get; set; } = 2600f;

	[JsonPropertyName("shock_seconds")]
	public float ShockSeconds { get; set; } = 1.5f;

	// ---- 伤害（范围） ----

	/// <summary>true = 以落点为球心、<see cref="FalloffRadius"/> 内按距离衰减；false = 全图固定 MaxDamage。</summary>
	[JsonPropertyName("falloff_enabled")]
	public bool FalloffEnabled { get; set; } = true;

	/// <summary>核心满伤（falloff_enabled=true 时）或全图固定伤害（false 时）。</summary>
	[JsonPropertyName("max_damage")]
	public int MaxDamage { get; set; } = 2000;

	/// <summary>边缘伤害（仅 falloff_enabled=true）。</summary>
	[JsonPropertyName("min_damage")]
	public int MinDamage { get; set; } = 250;

	/// <summary>伤害半径（≈50m）</summary>
	[JsonPropertyName("falloff_radius")]
	public float FalloffRadius { get; set; } = 2600f;

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

	// ---- 法阵刻画（SigilParams）：穹顶符文带与天空法阵共用 ----

	[JsonPropertyName("sigil_density")]
	public float SigilDensity { get; set; } = 1f;

	/// <summary>法阵线条基础宽度。</summary>
	[JsonPropertyName("sigil_width")]
	public float SigilWidth { get; set; } = 2.4f;

	[JsonPropertyName("rune_ticks")]
	public int RuneTicks { get; set; } = 84;

	[JsonPropertyName("tick_ring_count")]
	public int TickRingCount { get; set; } = 42;

	[JsonPropertyName("star_points")]
	public int StarPoints { get; set; } = 5;

	[JsonPropertyName("star_skip")]
	public int StarSkip { get; set; } = 2;

	[JsonPropertyName("polygon_sides")]
	public int PolygonSides { get; set; } = 8;

	[JsonPropertyName("double_line")]
	public bool DoubleLine { get; set; } = true;

	[JsonPropertyName("sigil_seed")]
	public int SigilSeed { get; set; }
}
