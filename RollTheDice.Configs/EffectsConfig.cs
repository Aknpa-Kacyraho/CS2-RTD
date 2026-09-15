using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

/// <summary>
/// 骰子粒子/特效总开关。想快速关闭全部特效（性能/排查）把 <c>enabled</c> 设为 false；
/// 只想关掉"脚底周期痕迹"而保留抽到/击杀/受击等触发特效，把 <c>trails</c> 设为 false。
/// 具体每个 dice 用哪种粒子在源码 <c>RollTheDice.Utils\DiceEffects.cs</c> 的设计表里改。
/// </summary>
public class EffectsConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>脚底周期痕迹（旧版是"常驻光环"，2026-09-15 重做后改为脚底周期小粒子）。</summary>
	[JsonPropertyName("trails")]
	public bool Trails { get; set; } = true;
}
