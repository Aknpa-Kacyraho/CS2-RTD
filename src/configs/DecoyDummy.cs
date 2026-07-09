using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DecoyDummyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("grenade_interval")] public float GrenadeInterval { get; set; } = 20f;
    }
}
