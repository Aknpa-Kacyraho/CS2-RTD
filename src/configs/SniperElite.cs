using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class SniperEliteConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("damage_multiplier")] public float DamageMultiplier { get; set; } = 3.0f;
    }
}
