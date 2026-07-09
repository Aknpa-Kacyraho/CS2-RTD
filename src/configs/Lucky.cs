using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class LuckyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("money_min")] public int MoneyMin { get; set; } = 50;
        [JsonPropertyName("money_max")] public int MoneyMax { get; set; } = 500;
        [JsonPropertyName("interval_min")] public float IntervalMin { get; set; } = 10f;
        [JsonPropertyName("interval_max")] public float IntervalMax { get; set; } = 30f;
    }
}
