using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class WeaponRouletteConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
