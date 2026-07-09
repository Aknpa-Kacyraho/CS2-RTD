using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class RagnarokConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("invul_duration")] public float InvulDuration { get; set; } = 30f;
        [JsonPropertyName("round_duration")] public float RoundDuration { get; set; } = 60f;
    }
}
