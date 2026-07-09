using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ImposterSyndromeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("decoy_interval")] public float DecoyInterval { get; set; } = 30f;
    }
}
