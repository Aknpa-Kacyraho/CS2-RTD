using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class BoneMaggotConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("mark_duration")] public float MarkDuration { get; set; } = 5f;
        [JsonPropertyName("reveal_through_walls")] public bool RevealThroughWalls { get; set; } = true;
    }
}
