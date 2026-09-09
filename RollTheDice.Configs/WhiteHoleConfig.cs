using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class WhiteHoleConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("duration")]
	public float Duration { get; set; } = 10f;

	[JsonPropertyName("push_radius")]
	public float PushRadius { get; set; } = 500f;

	[JsonPropertyName("enemy_push_strength")]
	public float EnemyPushStrength { get; set; } = 300f;

	[JsonPropertyName("teammate_pull_strength")]
	public float TeammatePullStrength { get; set; } = 150f;

	[JsonPropertyName("entity_push_strength")]
	public float EntityPushStrength { get; set; } = 400f;
}
