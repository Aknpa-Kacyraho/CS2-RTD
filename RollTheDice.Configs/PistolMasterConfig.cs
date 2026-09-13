using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PistolMasterConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("damage_multiplier")]
	public float DamageMultiplier { get; set; } = 2f;

	[JsonPropertyName("kill_reward")]
	public int KillReward { get; set; } = 300;
}
