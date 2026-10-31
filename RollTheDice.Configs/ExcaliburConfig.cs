using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ExcaliburConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>按满蓄所需秒数（按住 E 期间累积）。</summary>
	[JsonPropertyName("charge_seconds")]
	public float ChargeSeconds { get; set; } = 3f;

	/// <summary>低于此秒数松手 = 取消（不消耗冷却、不造成伤害），避免 E 键误触惩罚。</summary>
	[JsonPropertyName("min_charge_seconds")]
	public float MinChargeSeconds { get; set; } = 0.6f;

	/// <summary>最低威力系数（提前松手的下限），威力 = min + (1-min) × 进度。</summary>
	[JsonPropertyName("min_damage_scale")]
	public float MinDamageScale { get; set; } = 0.35f;

	/// <summary>满蓄的一次性伤害（每个目标只结算一次）。</summary>
	[JsonPropertyName("damage")]
	public int Damage { get; set; } = 500;

	/// <summary>友伤比例（0 = 不伤队友；1 = 与敌人同额）。施法者自己始终免疫。</summary>
	[JsonPropertyName("friendly_fire_scale")]
	public float FriendlyFireScale { get; set; } = 1f;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 90f;

	/// <summary>蓄力期间自身减速比例（0.5 = 移速 ×0.5）。</summary>
	[JsonPropertyName("slow_percent")]
	public float SlowPercent { get; set; } = 0.5f;

	/// <summary>命中击退初速（沿光柱方向，略带上抛）。</summary>
	[JsonPropertyName("knockback_speed")]
	public float KnockbackSpeed { get; set; } = 600f;

	[JsonPropertyName("blind_seconds")]
	public float BlindSeconds { get; set; } = 0.5f;

	[JsonPropertyName("blind_alpha")]
	public float BlindAlpha { get; set; } = 255f;

	/// <summary>光带长度（穿墙，不受地形截断）。</summary>
	[JsonPropertyName("beam_length")]
	public float BeamLength { get; set; } = 2600f;

	/// <summary>主带视觉直径 = 伤害直径（所见即所伤）。</summary>
	[JsonPropertyName("beam_width")]
	public float BeamWidth { get; set; } = 320f;

	/// <summary>最外层"光浪"宽度（纯视觉，不判伤——原作的巨大光带感）。</summary>
	[JsonPropertyName("beam_outer_width")]
	public float BeamOuterWidth { get; set; } = 620f;

	/// <summary>次外层辉光宽度。</summary>
	[JsonPropertyName("beam_glow_width")]
	public float BeamGlowWidth { get; set; } = 440f;

	/// <summary>亮层宽度。</summary>
	[JsonPropertyName("beam_bright_width")]
	public float BeamBrightWidth { get; set; } = 190f;

	/// <summary>白芯宽度。</summary>
	[JsonPropertyName("beam_core_width")]
	public float BeamCoreWidth { get; set; } = 80f;

	[JsonPropertyName("beam_width_start")]
	public float BeamWidthStart { get; set; } = 40f;

	/// <summary>喷涌：光带从剑尖延伸到全长的用时（"光之奔流"而非生长）。</summary>
	[JsonPropertyName("beam_surge_seconds")]
	public float BeamSurgeSeconds { get; set; } = 0.15f;

	[JsonPropertyName("beam_expand_seconds")]
	public float BeamExpandSeconds { get; set; } = 0.15f;

	/// <summary>喷涌到位后的保持时长（之后收缩淡出 0.3s）。</summary>
	[JsonPropertyName("beam_life_seconds")]
	public float BeamLifeSeconds { get; set; } = 0.9f;

	/// <summary>前端"光之断层"：垂直于光带的宽光墙（切断一切的感觉）。</summary>
	[JsonPropertyName("beam_wall_height")]
	public float BeamWallHeight { get; set; } = 760f;

	[JsonPropertyName("beam_wall_width")]
	public float BeamWallWidth { get; set; } = 34f;

	/// <summary>擦地光浪：沿光带轴线依次点亮的组数（原作"光浪刮过地面"）。</summary>
	[JsonPropertyName("ground_wave_count")]
	public int GroundWaveCount { get; set; } = 8;

	[JsonPropertyName("ground_wave_length")]
	public float GroundWaveLength { get; set; } = 520f;

	[JsonPropertyName("ground_wave_width")]
	public float GroundWaveWidth { get; set; } = 28f;

	/// <summary>奔流光环：数量 / 每环段数 / 半径 / 沿轴线推进速度。</summary>
	[JsonPropertyName("ring_count")]
	public int RingCount { get; set; } = 6;

	[JsonPropertyName("ring_segments")]
	public int RingSegments { get; set; } = 16;

	[JsonPropertyName("ring_radius")]
	public float RingRadius { get; set; } = 230f;

	[JsonPropertyName("ring_speed")]
	public float RingSpeed { get; set; } = 1.5f;

	/// <summary>满蓄剑长（段槽位沿剑轴按当前剑长分布）。</summary>
	[JsonPropertyName("sword_length")]
	public float SwordLength { get; set; } = 650f;

	/// <summary>起手剑长（随进度伸展到 sword_length）。</summary>
	[JsonPropertyName("sword_charge_length")]
	public float SwordChargeLength { get; set; } = 100f;

	/// <summary>白芯满蓄宽度。</summary>
	[JsonPropertyName("sword_core_width")]
	public float SwordCoreWidth { get; set; } = 11f;

	/// <summary>外辉光满蓄宽度。</summary>
	[JsonPropertyName("sword_glow_width")]
	public float SwordGlowWidth { get; set; } = 52f;

	/// <summary>蓄力时剑顶的冲天光柱（"引星之光"）：高度与宽度。</summary>
	[JsonPropertyName("top_pillar_height")]
	public float TopPillarHeight { get; set; } = 2600f;

	[JsonPropertyName("top_pillar_width")]
	public float TopPillarWidth { get; set; } = 26f;

	/// <summary>第二层法阵（外层逆行）：风格 / 起始半径 / 收拢半径 / 自转（负 = 反向）。</summary>
	[JsonPropertyName("sigil2_style")]
	public string Sigil2Style { get; set; } = "astrolabe";

	[JsonPropertyName("sigil2_radius_start")]
	public float Sigil2RadiusStart { get; set; } = 300f;

	[JsonPropertyName("sigil2_radius_end")]
	public float Sigil2RadiusEnd { get; set; } = 60f;

	[JsonPropertyName("sigil2_spin")]
	public float Sigil2Spin { get; set; } = -0.9f;

	/// <summary>拼凑段数（光丝飞来嵌合成剑）。</summary>
	[JsonPropertyName("segment_count")]
	public int SegmentCount { get; set; } = 12;

	/// <summary>
	/// 实体剑模型（**空 = 退回 CBeam 剑身**）。2026-10-31 起默认禁用：
	/// 自制模型 / 换模会小概率触发客户端实体表崩溃，统一不再使用。
	/// </summary>
	[JsonPropertyName("sword_model")]
	public string SwordModel { get; set; } = "";

	/// <summary>实体剑缩放（7.6 ≈ 650 单位长，对齐 <c>sword_length</c>）。</summary>
	[JsonPropertyName("sword_model_scale")]
	public float SwordModelScale { get; set; } = 7.6f;

	/// <summary>
	/// 模型沿剑轴前移量（**模型单位**，会乘上缩放）：模型原点在剑柄内侧 22.6 单位，
	/// 前移后剑柄正好落在剑基座上（否则剑柄会插到脚下/身体里）。
	/// </summary>
	[JsonPropertyName("sword_model_offset")]
	public float SwordModelOffset { get; set; } = 22.6f;

	/// <summary>实体剑绕自身长轴的滚转（度）：调剑刃朝向。</summary>
	[JsonPropertyName("sword_model_roll")]
	public float SwordModelRoll { get; set; }

	/// <summary>剑尖朝反了就把这个打开（沿剑轴翻转 180°）。</summary>
	[JsonPropertyName("sword_model_flip")]
	public bool SwordModelFlip { get; set; }

	/// <summary>蓄力到多少进度时实体剑出现（0 = 一开始就在，1 = 满蓄才出现）。</summary>
	[JsonPropertyName("sword_model_spawn_progress")]
	public float SwordModelSpawnProgress { get; set; } = 0.55f;

	/// <summary>是否保留 CBeam 剑格/剑尖十字/符文刻度（有实体剑时默认关，别让线条糊在模型上）。</summary>
	[JsonPropertyName("sword_model_ornaments")]
	public bool SwordModelOrnaments { get; set; }

	[JsonPropertyName("segment_width")]
	public float SegmentWidth { get; set; } = 2.5f;

	/// <summary>汇聚光丝数量（随进度从 ~1/4 增到全量）。</summary>
	[JsonPropertyName("streak_count")]
	public int StreakCount { get; set; } = 32;

	/// <summary>汇聚半径（光丝从多远被"吸"进来）。</summary>
	[JsonPropertyName("streak_radius")]
	public float StreakRadius { get; set; } = 450f;

	[JsonPropertyName("sigil_radius_start")]
	public float SigilRadiusStart { get; set; } = 420f;

	[JsonPropertyName("sigil_radius_end")]
	public float SigilRadiusEnd { get; set; } = 90f;

	[JsonPropertyName("sigil_density")]
	public float SigilDensity { get; set; } = 1f;

	[JsonPropertyName("sigil_width")]
	public float SigilWidth { get; set; } = 2.2f;

	[JsonPropertyName("sigil_spin")]
	public float SigilSpin { get; set; } = 0.6f;

	[JsonPropertyName("shake_amplitude")]
	public float ShakeAmplitude { get; set; } = 22f;

	[JsonPropertyName("shake_frequency")]
	public float ShakeFrequency { get; set; } = 40f;

	[JsonPropertyName("shake_duration")]
	public float ShakeDuration { get; set; } = 1.2f;

	/// <summary>震动半径（0 = 全图）。</summary>
	[JsonPropertyName("shake_radius")]
	public float ShakeRadius { get; set; } = 3000f;

	/// <summary>蓄力起手音（从施法者身上 3D 发声）。</summary>
	[JsonPropertyName("charge_sound")]
	public string ChargeSound { get; set; } = "sounds/touhou/xrole/sanae/laser_draw.vsnd";

	/// <summary>蓄力循环音（音量随进度爬升）。</summary>
	[JsonPropertyName("charge_loop_sound")]
	public string ChargeLoopSound { get; set; } = "sounds/touhou/xrole/sanae/laser_active.vsnd";

	[JsonPropertyName("charge_loop_interval")]
	public float ChargeLoopInterval { get; set; } = 0.7f;

	/// <summary>满蓄瞬间的全服就绪音。</summary>
	[JsonPropertyName("ready_sound")]
	public string ReadySound { get; set; } = "sounds/touhou/powerup.vsnd";

	[JsonPropertyName("ready_sound_volume")]
	public float ReadySoundVolume { get; set; } = 0.6f;

	/// <summary>倾泻瞬间的全服音效。</summary>
	[JsonPropertyName("burst_sound")]
	public string BurstSound { get; set; } = "sounds/touhou/xrole/marisa/explode_laser_fire.vsnd";

	[JsonPropertyName("burst_sound_volume")]
	public float BurstSoundVolume { get; set; } = 0.9f;
}
