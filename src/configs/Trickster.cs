using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class TricksterConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
