using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class MosquitoConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("size_scale")]
	public float SizeScale { get; set; } = 0.2f;

	[JsonPropertyName("health")]
	public int Health { get; set; } = 20;

	[JsonPropertyName("slow")]
	public float Slow { get; set; } = 0.3f;

	[JsonPropertyName("slow_seconds")]
	public float SlowSeconds { get; set; } = 2f;
}