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

	/// <summary>底盘内部的徽记风格：`none` = 只有轮盘骨架（默认，最接近参考图）；也可 arcane / ward / astrolabe / sun。</summary>
	[JsonPropertyName("dome_style")]
	public string DomeStyle { get; set; } = "none";

	/// <summary>同轴圆盘层数（含底盘）：3 = 底/中/顶，2 = 底/顶。</summary>
	[JsonPropertyName("dome_discs")]
	public int DomeDiscs { get; set; } = 3;

	/// <summary>空间锚定斜线数量（从底盘边缘斜拉向高空）。</summary>
	[JsonPropertyName("dome_anchors")]
	public int DomeAnchors { get; set; } = 8;

	/// <summary>穹顶从地面长到顶点所用的秒数。</summary>
	[JsonPropertyName("dome_grow_seconds")]
	public float DomeGrowSeconds { get; set; } = 2.4f;

	[JsonPropertyName("dome_spin")]
	public float DomeSpin { get; set; } = 0.35f;

	/// <summary>倒数末尾亮度脉冲持续的秒数（发动信号）。</summary>
	[JsonPropertyName("dome_pulse_seconds")]
	public float DomePulseSeconds { get; set; } = 3f;

	/// <summary>经线螺旋量（弧度，沿纬度扭过去）：0 = 普通网格笼，0.5~0.8 = 优雅的螺旋笼。</summary>
	[JsonPropertyName("dome_twist")]
	public float DomeTwist { get; set; } = 0.55f;

	/// <summary>顶点顶冠（水平环 + 罗盘星 + 刻度环），穹顶长完后出现。</summary>
	[JsonPropertyName("dome_crown")]
	public bool DomeCrown { get; set; } = true;

	/// <summary>底盘外缘的立式符文带 + 外唇（图 2「外圈符文环带」→ 盘看起来有厚度）。</summary>
	[JsonPropertyName("dome_rim")]
	public bool DomeRim { get; set; } = true;

	/// <summary>4 个倾斜侧阵门（图 1/2/3 东/西/南/北侧阵门：独立倾斜小盘）。</summary>
	[JsonPropertyName("dome_gates")]
	public bool DomeGates { get; set; } = true;

	/// <summary>侧阵门倾角（度）：0 = 水平盘，90 = 竖直（法线朝外上方）。</summary>
	[JsonPropertyName("dome_gate_tilt")]
	public float DomeGateTilt { get; set; } = 68f;

	/// <summary>侧阵门半径系数（× dome_radius）。</summary>
	[JsonPropertyName("dome_gate_size")]
	public float DomeGateSize { get; set; } = 0.22f;

	/// <summary>环形轨道带 + 弧形连接臂（图 2「弧形轨道 / 弧形连接轨道」）。</summary>
	[JsonPropertyName("dome_tracks")]
	public bool DomeTracks { get; set; } = true;

	/// <summary>中心主轴 + 立体核心 + 贯通轴线（图 3「主轴连接层」/「东西南北轴」）。</summary>
	[JsonPropertyName("dome_axis")]
	public bool DomeAxis { get; set; } = true;

	// ---- 阶段二：头顶天空法阵群 ----

	[JsonPropertyName("sky_enabled")]
	public bool SkyEnabled { get; set; } = true;

	/// <summary>法阵层数（"数十个"）。</summary>
	[JsonPropertyName("sky_count")]
	public int SkyCount { get; set; } = 12;

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

	/// <summary>法阵风格：mixed = 每层轮换（arcane/ward/astrolabe/sun），也可固定成其中一种。</summary>
	[JsonPropertyName("sky_style")]
	public string SkyStyle { get; set; } = "mixed";

	/// <summary>整体细节系数（0.2~1.5）：越小每层束数越少，是"卡不卡"的第一旋钮。</summary>
	[JsonPropertyName("sky_detail")]
	public float SkyDetail { get; set; } = 0.75f;

	/// <summary>参与自转的层数（0 = 全静态）。只有这些层走每 tick 更新。</summary>
	[JsonPropertyName("sky_animated_layers")]
	public int SkyAnimatedLayers { get; set; } = 4;

	/// <summary>整片天空法阵的 CBeam 预算上限（只限"风格刻画"，装置骨架不计入），超出后自动给后续层降采样。</summary>
	[JsonPropertyName("sky_beam_budget")]
	public int SkyBeamBudget { get; set; } = 1600;

	/// <summary>大层外缘的立式短齿（"盘有厚度"，从下往上看是盘的下沿）。</summary>
	[JsonPropertyName("sky_rim")]
	public bool SkyRim { get; set; } = true;

	/// <summary>大层之间的斜向连接骨架（跨过中间小层 → 整片法阵成为螺旋漏斗塔）。</summary>
	[JsonPropertyName("sky_spines")]
	public bool SkySpines { get; set; } = true;

	/// <summary>中央主轴（贯穿最低层→最高层）+ 大层中心轴承环。</summary>
	[JsonPropertyName("sky_axis")]
	public bool SkyAxis { get; set; } = true;

	// ---- 通天光柱 / 地面冲击环 ----

	/// <summary>光柱从地面向上延伸的高度（要盖过最高层法阵）。</summary>
	[JsonPropertyName("pillar_height")]
	public float PillarHeight { get; set; } = 4200f;

	/// <summary>光柱最终半径（直径 = 2×，默认 1800u ≈ 36m）——**同时也是灼烧判定圆**（所见即所伤）。</summary>
	[JsonPropertyName("pillar_radius")]
	public float PillarRadius { get; set; } = 1800f;

	/// <summary>光柱存活总时长（含扩大期）。</summary>
	[JsonPropertyName("pillar_life")]
	public float PillarLife { get; set; } = 5f;

	/// <summary>
	/// 光柱从"细"扩到 <see cref="PillarRadius"/> 所用的秒数（ease-out：先炸开再稳住）。
	/// ★ 2026-09-22 用户明确要**保留这个扩张过程**（"光柱壳"那版的难看是因为肋条/箍环跟主体不同步，
	/// 不是扩张本身）→ 默认 0.9；想改成"瞬间全宽"就设 0.12。
	/// </summary>
	[JsonPropertyName("beam_expand_seconds")]
	public float BeamExpandSeconds { get; set; } = 0.9f;

	/// <summary>光柱起始半径（细柱）。</summary>
	[JsonPropertyName("beam_radius_start")]
	public float BeamRadiusStart { get; set; } = 120f;

	/// <summary>
	/// 只放大**视觉**宽度的倍率（伤害圆仍走 <see cref="PillarRadius"/>）：
	/// CBeam 是软边 sprite，亮芯远小于标称宽度，觉得柱子"细"就往上加（1.5~2.5），
	/// 但要注意 >1 时"看起来在柱里却不在伤害圆内"。
	/// </summary>
	[JsonPropertyName("beam_visual_scale")]
	public float BeamVisualScale { get; set; } = 1f;

	/// <summary>柱内每秒伤害（连续掉血，靠累积小数结算，不会被 tick 取整吃掉）。</summary>
	[JsonPropertyName("beam_dps")]
	public int BeamDps { get; set; } = 200;

	/// <summary>光晕倍率（spawn 前设的有效）；若开着 <see cref="BeamFlags"/> 的 HALOBEAM 更明显。</summary>
	[JsonPropertyName("beam_halo_scale")]
	public float BeamHaloScale { get; set; } = 1f;

	/// <summary>
	/// Source beam flags 位掩码（spawn 前设的有效）：32768=HALOBEAM（柔光晕）、16=SINENOISE（蛇形噪声）、
	/// 4|8=FADEIN|FADEOUT（首尾淡出）、32=SOLID。0 = 默认。
	/// </summary>
	[JsonPropertyName("beam_flags")]
	public int BeamFlags { get; set; }

	/// <summary>光柱纹理沿柱身滚动的速度（spawn 前设的有效，0 = 不动）。</summary>
	[JsonPropertyName("beam_scroll_speed")]
	public float BeamScrollSpeed { get; set; } = 220f;

	/// <summary>
	/// 法阵自转的更新间隔（tick）：2 = 隔一 tick（默认，**每 tick 的 CBeam 网络更新直接减半**，
	/// 32Hz 的自转看不出差别）；觉得转动发涩就设 1。
	/// </summary>
	[JsonPropertyName("sigil_animation_stride")]
	public int SigilAnimationStride { get; set; } = 2;

	[JsonPropertyName("shock_rings")]
	public int ShockRings { get; set; } = 3;

	[JsonPropertyName("shock_radius")]
	public float ShockRadius { get; set; } = 2600f;

	[JsonPropertyName("shock_seconds")]
	public float ShockSeconds { get; set; } = 1.5f;

	// ---- 伤害（范围） ----

	/// <summary>
	/// 额外的"冲击瞬间"伤害（默认**关**）：现在主伤害是光柱内的持续灼烧（<see cref="BeamDps"/>）。
	/// 打开 = 引爆瞬间再按距离衰减扣一次血（旧行为），会和灼烧叠加，注意别做太狠。
	/// </summary>
	[JsonPropertyName("falloff_enabled")]
	public bool FalloffEnabled { get; set; }

	/// <summary>核心满伤（falloff_enabled=true 时）或全图固定伤害（false 时）。</summary>
	[JsonPropertyName("max_damage")]
	public int MaxDamage { get; set; } = 2000;

	/// <summary>边缘伤害（仅 falloff_enabled=true）。</summary>
	[JsonPropertyName("min_damage")]
	public int MinDamage { get; set; } = 250;

	/// <summary>（已弃用）旧的范围伤害半径：现在光柱半径 = <see cref="PillarRadius"/>，不再用这个。</summary>
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
