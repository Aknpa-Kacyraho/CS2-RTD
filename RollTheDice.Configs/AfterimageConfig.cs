using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class AfterimageConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("record_interval_min")]
	public float RecordIntervalMin { get; set; } = 3f;

	[JsonPropertyName("record_interval_max")]
	public float RecordIntervalMax { get; set; } = 6f;

	[JsonPropertyName("max_shadows")]
	public int MaxShadows { get; set; } = 3;

	[JsonPropertyName("recall_cooldown")]
	public float RecallCooldown { get; set; } = 4f;

	[JsonPropertyName("smoke_on_recall")]
	public bool SmokeOnRecall { get; set; } = true;
}
