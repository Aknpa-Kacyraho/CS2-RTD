using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PickpocketConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("steal_percent_min")] public float StealPercentMin { get; set; } = 0.10f;
        [JsonPropertyName("steal_percent_max")] public float StealPercentMax { get; set; } = 0.35f;
    }
}
