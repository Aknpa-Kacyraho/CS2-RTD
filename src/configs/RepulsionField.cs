using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class RepulsionFieldConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 8.0f;
        [JsonPropertyName("speed_multiplier")] public float SpeedMultiplier { get; set; } = 1.5f;
    }
}
