using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class KarmaConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("speed_multiplier")] public float SpeedMultiplier { get; set; } = 1.5f;
        [JsonPropertyName("hp_per_second")] public int HpPerSecond { get; set; } = 1;
    }
}
