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
	public float KillTime { get; set; } = 100f;
}
