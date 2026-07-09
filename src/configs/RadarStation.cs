using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class RadarStationConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
