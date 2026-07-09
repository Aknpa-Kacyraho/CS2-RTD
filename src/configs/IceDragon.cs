using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class IceDragonConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("bonus_hp")] public int BonusHP { get; set; } = 333;
        [JsonPropertyName("bonus_armor")] public int BonusArmor { get; set; } = 222;
        [JsonPropertyName("freeze_duration")] public float FreezeDuration { get; set; } = 0.2f;
    }
}
