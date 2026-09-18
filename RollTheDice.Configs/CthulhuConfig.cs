using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class CthulhuConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("speed_loss_per_sec")]
	public float SpeedLossPerSec { get; set; } = 0.01f;

	[JsonPropertyName("hp_loss_per_sec")]
	public int HpLossPerSec { get; set; } = 1;

	[JsonPropertyName("kill_time")]
	public float KillTime { get; set; } = 80f;

	/// <summary>持有者获得的减伤，替代原先"1HP+定身"的自残设计。</summary>
	[JsonPropertyName("damage_reduction")]
	public float DamageReduction { get; set; } = 0.6f;
}
