using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class TaotieConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("hp_per_eat")] public int HpPerEat { get; set; } = 100;
    }
}
