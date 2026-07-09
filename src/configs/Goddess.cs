using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class GoddessConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
