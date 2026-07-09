using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class PhoenixConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("invul_duration")] public float InvulDuration { get; set; } = 10f;
        [JsonPropertyName("heal_hp")] public int HealHP { get; set; } = 444;
        [JsonPropertyName("heal_armor")] public int HealArmor { get; set; } = 444;
        [JsonPropertyName("explosion_radius")] public float ExplosionRadius { get; set; } = 500f;
        [JsonPropertyName("explosion_damage")] public int ExplosionDamage { get; set; } = 80;
        [JsonPropertyName("float_speed")] public float FloatSpeed { get; set; } = 30f;
    }
}
