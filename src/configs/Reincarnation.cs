using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class ReincarnationConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("max_stacks")] public int MaxStacks { get; set; } = 2;
    }
}
