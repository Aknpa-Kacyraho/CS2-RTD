using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class SmokeVisionConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("glow_color_t_red")] public int GlowColorTRed { get; set; } = 255;
        [JsonPropertyName("glow_color_t_green")] public int GlowColorTGreen { get; set; } = 0;
        [JsonPropertyName("glow_color_t_blue")] public int GlowColorTBlue { get; set; } = 0;
        [JsonPropertyName("glow_color_ct_red")] public int GlowColorCTRed { get; set; } = 0;
        [JsonPropertyName("glow_color_ct_green")] public int GlowColorCTGreen { get; set; } = 0;
        [JsonPropertyName("glow_color_ct_blue")] public int GlowColorCTBlue { get; set; } = 255;
    }
}
