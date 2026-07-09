using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class MimicConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
