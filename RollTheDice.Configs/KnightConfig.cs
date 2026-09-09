using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class KnightConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("hp_transfer")]
	public int HpTransfer { get; set; } = 50;

	[JsonPropertyName("transfer_interval")]
	public float TransferInterval { get; set; } = 10f;

	[JsonPropertyName("self_max_health")]
	public int SelfMaxHealth { get; set; } = 300;
}
