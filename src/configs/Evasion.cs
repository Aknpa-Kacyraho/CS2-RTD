using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class EvasionConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("dodge_chance_min")] public float DodgeChanceMin { get; set; } = 0.20f;
        [JsonPropertyName("dodge_chance_max")] public float DodgeChanceMax { get; set; } = 0.66f;
    }
}
