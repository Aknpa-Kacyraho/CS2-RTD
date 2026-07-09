using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class NirvanaConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_chance")] public float MinChance { get; set; } = 0.30f;
        [JsonPropertyName("max_chance")] public float MaxChance { get; set; } = 0.50f;
        [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 0.5f;
    }
}
