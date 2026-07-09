using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class SingularityConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("duration")] public float Duration { get; set; } = 15f;
        [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 60f;
        [JsonPropertyName("pull_strength")] public float PullStrength { get; set; } = 500f;
    }
}
