using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class GiantConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("size_scale")] public float SizeScale { get; set; } = 4.0f;
        [JsonPropertyName("health_multiplier")] public float HealthMultiplier { get; set; } = 4.0f;
    }
}
