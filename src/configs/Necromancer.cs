using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class NecromancerConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("revive_hp_cost")] public int ReviveHPCost { get; set; } = 50;
    }
}
