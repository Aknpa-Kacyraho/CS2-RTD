using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DeafConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("wallhack_duration")] public float WallhackDuration { get; set; } = 4f;
        [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 25f;
    }
}
