using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DeathKnightCompleteConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("bonus_hp")] public int BonusHP { get; set; } = 333;
        [JsonPropertyName("bonus_armor")] public int BonusArmor { get; set; } = 333;
        [JsonPropertyName("initial_damage_reduction")] public float InitialDamageReduction { get; set; } = 0.50f;
        [JsonPropertyName("max_damage_reduction")] public float MaxDamageReduction { get; set; } = 0.99f;
        [JsonPropertyName("knife_heal_per_sec")] public int KnifeHealPerSec { get; set; } = 3;
    }
}
