using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class NoRecoilConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("damage_multiplier")] public float DamageMultiplier { get; set; } = 1.1f;
    }
}
