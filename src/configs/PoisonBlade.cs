using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PoisonBladeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("chance_min")] public float ChanceMin { get; set; } = 0.20f;
        [JsonPropertyName("chance_max")] public float ChanceMax { get; set; } = 0.50f;
        [JsonPropertyName("damage_per_tick_min")] public int DamagePerTickMin { get; set; } = 1;
        [JsonPropertyName("damage_per_tick_max")] public int DamagePerTickMax { get; set; } = 5;
        [JsonPropertyName("tick_count")] public int TickCount { get; set; } = 4;
        [JsonPropertyName("tick_interval")] public float TickInterval { get; set; } = 1.0f;
    }
}
