using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class GuillotineConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("execute_hp_threshold")]
	public float ExecuteHpThreshold { get; set; } = 0.35f;
}
