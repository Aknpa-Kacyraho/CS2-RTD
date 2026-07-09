using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class FireballConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("damage_min")] public int DamageMin { get; set; } = 80;
        [JsonPropertyName("damage_max")] public int DamageMax { get; set; } = 200;
        [JsonPropertyName("radius_multiplier")] public float RadiusMultiplier { get; set; } = 1.5f;
    }
}
