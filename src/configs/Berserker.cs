using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class BerserkerConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("max_multiplier")] public float MaxMultiplier { get; set; } = 4.0f;
    }
}
