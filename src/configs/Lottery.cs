using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class LotteryConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("money_min")] public int MoneyMin { get; set; } = 0;
        [JsonPropertyName("money_max")] public int MoneyMax { get; set; } = 5000;
    }
}
