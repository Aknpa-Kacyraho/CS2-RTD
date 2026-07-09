using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DeathKnightConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("reduction_cap")] public float ReductionCap { get; set; } = 0.99f;
        [JsonPropertyName("hp_regen")] public int HpRegen { get; set; } = 1;
    }
}
