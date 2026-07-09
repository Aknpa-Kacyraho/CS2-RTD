using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class CoronaConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("delay")] public float Delay { get; set; } = 45f;
        [JsonPropertyName("respawn_hp")] public int RespawnHP { get; set; } = 200;
        [JsonPropertyName("respawn_armor")] public int RespawnArmor { get; set; } = 200;
        [JsonPropertyName("fire_dps")] public int FireDps { get; set; } = 5;
        [JsonPropertyName("fire_duration")] public float FireDuration { get; set; } = 5f;
    }
}
