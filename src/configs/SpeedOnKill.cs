using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class SpeedOnKillConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("speed_multiplier_min")] public float SpeedMultiplierMin { get; set; } = 1.44f;
        [JsonPropertyName("speed_multiplier_max")] public float SpeedMultiplierMax { get; set; } = 1.92f;
        [JsonPropertyName("duration_min")] public float DurationMin { get; set; } = 3.6f;
        [JsonPropertyName("duration_max")] public float DurationMax { get; set; } = 9.6f;
    }
}
