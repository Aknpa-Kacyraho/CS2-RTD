using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ThunderChainConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("chain_min")] public int ChainMin { get; set; } = 2;
        [JsonPropertyName("chain_max")] public int ChainMax { get; set; } = 4;
        [JsonPropertyName("chain_range")] public float ChainRange { get; set; } = 900f;
        [JsonPropertyName("initial_damage_min")] public int InitialDamageMin { get; set; } = 60;
        [JsonPropertyName("initial_damage_max")] public int InitialDamageMax { get; set; } = 90;
        [JsonPropertyName("damage_decay")] public float DamageDecay { get; set; } = 0.3f;
    }
}
