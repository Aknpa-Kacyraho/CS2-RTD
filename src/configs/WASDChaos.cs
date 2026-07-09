using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class WASDChaosConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_interval")] public float MinInterval { get; set; } = 2f;
        [JsonPropertyName("max_interval")] public float MaxInterval { get; set; } = 5f;
        [JsonPropertyName("yaw_shift")] public float YawShift { get; set; } = 90f;
    }
}
