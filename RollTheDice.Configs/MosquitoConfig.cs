using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class MosquitoConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("size_scale")]
	public float SizeScale { get; set; } = 0.2f;

	[JsonPropertyName("health")]
	public int Health { get; set; } = 22;
}
