using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class SwordSaintConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
