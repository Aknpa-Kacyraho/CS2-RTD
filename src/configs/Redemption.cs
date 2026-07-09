using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class RedemptionConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("damage_bonus")] public float DamageBonus { get; set; } = 0.4f;
    }
}
