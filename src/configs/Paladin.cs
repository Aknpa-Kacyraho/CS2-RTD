using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PaladinConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("bonus_armor")] public int BonusArmor { get; set; } = 200;
        [JsonPropertyName("rage_speed")] public float RageSpeed { get; set; } = 1.5f;
        [JsonPropertyName("rage_damage")] public float RageDamage { get; set; } = 1.3f;
    }
}
