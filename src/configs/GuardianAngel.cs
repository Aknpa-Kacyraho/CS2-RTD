using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class GuardianAngelConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("restore_health")] public int RestoreHealth { get; set; } = 50;
        [JsonPropertyName("invincibility_seconds")] public float InvincibilitySeconds { get; set; } = 2.0f;
    }
}
