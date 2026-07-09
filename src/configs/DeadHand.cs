using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DeadHandConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("self_damage")] public int SelfDamage { get; set; } = 15;
    }
}
