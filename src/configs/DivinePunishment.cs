using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DivinePunishmentConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("kills_required")] public int KillsRequired { get; set; } = 2;
        [JsonPropertyName("damage")] public int Damage { get; set; } = 80;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 250f;
        [JsonPropertyName("warning_delay")] public float WarningDelay { get; set; } = 1.0f;
    }
}
