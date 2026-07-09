using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class GodConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
