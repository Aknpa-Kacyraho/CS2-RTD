using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class HermitConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
