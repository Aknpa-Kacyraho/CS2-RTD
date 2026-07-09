using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ChaosStormConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("interval")] public float Interval { get; set; } = 45f;
    }
}
