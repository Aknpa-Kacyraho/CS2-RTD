using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ReturnToSenderConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;

        [JsonPropertyName("min_chance")] public int MinChance { get; set; } = 30;

        [JsonPropertyName("max_chance")] public int MaxChance { get; set; } = 70;
    }
}
