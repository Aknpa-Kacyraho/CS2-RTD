using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ShadowWarriorConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 12f;
        [JsonPropertyName("clone_lifetime")] public float CloneLifetime { get; set; } = 6f;
        [JsonPropertyName("clone_distance")] public float CloneDistance { get; set; } = 120f;
        [JsonPropertyName("alpha")] public int Alpha { get; set; } = 90;
    }
}
