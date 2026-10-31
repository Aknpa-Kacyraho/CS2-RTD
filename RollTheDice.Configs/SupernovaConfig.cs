using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class SupernovaConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration_seconds")]
	public float DurationSeconds { get; set; } = 5f;

	/// <summary>开火前的充能（炮体组装 / 导轨张开）时长；0 = 立即开火。</summary>
	[JsonPropertyName("charge_seconds")]
	public float ChargeSeconds { get; set; } = 0.5f;

	[JsonPropertyName("cooldown_seconds")]
	public float CooldownSeconds { get; set; } = 30f;

	[JsonPropertyName("range")]
	public float Range { get; set; } = 1200f;

	[JsonPropertyName("damage_per_second")]
	public float DamagePerSecond { get; set; } = 100f;

	[JsonPropertyName("hit_radius")]
	public float HitRadius { get; set; } = 70f;

	[JsonPropertyName("slow_percent")]
	public float SlowPercent { get; set; } = 0.5f;

	/// <summary>主层（青色）光束宽度。</summary>
	[JsonPropertyName("beam_width_outer")]
	public float BeamWidthOuter { get; set; } = 20f;

	/// <summary>内层白色光束宽度（白芯）。</summary>
	[JsonPropertyName("beam_width_inner")]
	public float BeamWidthInner { get; set; } = 9f;

	/// <summary>最外层淡蓝辉光的宽度倍率（× beam_width_outer）：1.5~3 像能量炮，越大越发散。</summary>
	[JsonPropertyName("glow_width_scale")]
	public float GlowWidthScale { get; set; } = 2.4f;

	/// <summary>电磁炮构造体总开关（关掉只剩光束）。</summary>
	[JsonPropertyName("gun_enabled")]
	public bool GunEnabled { get; set; } = true;

	/// <summary>炮体整体缩放（1 = 总长 190 单位 ≈ 4.8m）。</summary>
	[JsonPropertyName("gun_scale")]
	public float GunScale { get; set; } = 1f;

	/// <summary>炮身相对胸口的右移（第一人称可见、不挡准星）。</summary>
	[JsonPropertyName("gun_side")]
	public float GunSide { get; set; } = 34f;

	/// <summary>炮身相对胸口的向下偏移。</summary>
	[JsonPropertyName("gun_drop")]
	public float GunDrop { get; set; } = 6f;

	/// <summary>炮尾相对胸口的前移。</summary>
	[JsonPropertyName("gun_forward")]
	public float GunForward { get; set; } = 46f;

	/// <summary>沿光束从炮口向末端循环推进的流光环数量（0 = 关）。</summary>
	[JsonPropertyName("ring_count")]
	public int RingCount { get; set; } = 3;

	/// <summary>流光环半径（随前进略收）。</summary>
	[JsonPropertyName("ring_radius")]
	public float RingRadius { get; set; } = 26f;

	/// <summary>流光环推进速度（每秒走完光束的倍数）。</summary>
	[JsonPropertyName("ring_speed")]
	public float RingSpeed { get; set; } = 0.45f;

	/// <summary>电磁脉冲间隔（秒；0 = 关）：一道亮段从炮口向外推进 + 炮口电光 = 连射感。</summary>
	[JsonPropertyName("pulse_interval")]
	public float PulseInterval { get; set; } = 0.35f;

	/// <summary>命中敌人时在其身上爆电火花（节流 0.15s）。</summary>
	[JsonPropertyName("hit_spark")]
	public bool HitSpark { get; set; } = true;

	/// <summary>命中反馈粒子（基础游戏粒子，无需工坊）。</summary>
	[JsonPropertyName("hit_particle")]
	public string HitParticle { get; set; } = "particles/generic_fx/fx_electricspark_glow.vpcf";

	/// <summary>炮口爆发粒子（基础游戏粒子，无需工坊）。</summary>
	[JsonPropertyName("muzzle_particle")]
	public string MuzzleParticle { get; set; } = "particles/ui/status_levels/ui_status_level_4_energycirc.vpcf";

	/// <summary>开火瞬间的全服音效（基础游戏 soundevent 名或 .vsnd 路径）。</summary>
	[JsonPropertyName("blast_sound")]
	public string BlastSound { get; set; } = "CSGO_EMP.Impact";

	[JsonPropertyName("blast_sound_volume")]
	public float BlastSoundVolume { get; set; } = 0.8f;
}
