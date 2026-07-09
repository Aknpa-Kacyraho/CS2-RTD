using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class BountyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("money_min")] public int MoneyMin { get; set; } = 500;
        [JsonPropertyName("money_max")] public int MoneyMax { get; set; } = 3000;
    }
}
