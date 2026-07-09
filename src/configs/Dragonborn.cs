using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class DragonbornConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("kills_required")] public int KillsRequired { get; set; } = 2;
        [JsonPropertyName("dragon_hp")] public int DragonHP { get; set; } = 300;
        [JsonPropertyName("dragon_armor")] public int DragonArmor { get; set; } = 300;
    }
}
