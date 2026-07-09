using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class AwakenerConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("kills_to_max")] public int KillsToMax { get; set; } = 2;
        [JsonPropertyName("start_damage_mult")] public float StartDamageMult { get; set; } = 0.5f;
        [JsonPropertyName("start_speed_mult")] public float StartSpeedMult { get; set; } = 0.7f;
        [JsonPropertyName("max_damage_mult")] public float MaxDamageMult { get; set; } = 2f;
        [JsonPropertyName("max_speed_mult")] public float MaxSpeedMult { get; set; } = 1.5f;
    }
}
