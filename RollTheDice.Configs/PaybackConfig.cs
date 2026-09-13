using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PaybackConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp")]
	public int Hp { get; set; } = 50;

	[JsonPropertyName("clear_money")]
	public bool ClearMoney { get; set; } = true;
}