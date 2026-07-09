using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class FateConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("orbit_death_time")] public float OrbitDeathTime { get; set; } = 60f;
        [JsonPropertyName("dice_luck_interval")] public float DiceLuckInterval { get; set; } = 30f;
        [JsonPropertyName("balance_hp_loss")] public int BalanceHpLoss { get; set; } = 1;
        [JsonPropertyName("balance_damage_bonus")] public float BalanceDamageBonus { get; set; } = 0.60f;
        [JsonPropertyName("compass_interval")] public float CompassInterval { get; set; } = 20f;
        [JsonPropertyName("compass_duration")] public float CompassDuration { get; set; } = 2f;
        [JsonPropertyName("web_invul_duration")] public float WebInvulDuration { get; set; } = 3f;
        [JsonPropertyName("darktide_freeze_duration")] public float DarktideFreezeDuration { get; set; } = 2f;
        [JsonPropertyName("darktide_slow_amount")] public float DarktideSlowAmount { get; set; } = 0.10f;
        [JsonPropertyName("dawn_freeze_duration")] public float DawnFreezeDuration { get; set; } = 2f;
        [JsonPropertyName("dawn_hp_penalty")] public int DawnHpPenalty { get; set; } = 50;
    }
}
