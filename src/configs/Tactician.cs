using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class TacticianConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("reveal_interval")] public float RevealInterval { get; set; } = 15.0f;
        [JsonPropertyName("reveal_duration")] public float RevealDuration { get; set; } = 1.0f;
    }
}
