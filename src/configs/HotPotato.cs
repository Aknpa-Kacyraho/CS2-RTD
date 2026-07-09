using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class HotPotatoConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("damage")] public int Damage { get; set; } = 4;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 300f;
    }
}
