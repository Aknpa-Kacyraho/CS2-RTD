using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class WheelOfFateConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("revive_chance")] public float ReviveChance { get; set; } = 0.66f;
    }
}
