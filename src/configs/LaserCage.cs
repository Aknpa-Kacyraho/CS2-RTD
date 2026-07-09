using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class LaserCageConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 120f;
        [JsonPropertyName("beam_count")] public int BeamCount { get; set; } = 6;
        [JsonPropertyName("damage_per_touch")] public int DamagePerTouch { get; set; } = 8;
        [JsonPropertyName("tick_interval")] public float TickInterval { get; set; } = 0.3f;
    }
}
