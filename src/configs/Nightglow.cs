using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class NightglowConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
