using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class InfoHoleConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
