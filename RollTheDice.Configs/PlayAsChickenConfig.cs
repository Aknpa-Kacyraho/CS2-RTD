using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class PlayAsChickenConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("sound_volume")]
	public float SoundVolume { get; set; } = 3f;

	[JsonPropertyName("min_sound_wait_time")]
	public int MinSoundWaitTime { get; set; } = 3;

	[JsonPropertyName("max_sound_wait_time")]
	public int MaxSoundWaitTime { get; set; } = 7;

	[JsonPropertyName("chicken_hp")]
	public int ChickenHp { get; set; } = 300;

	[JsonPropertyName("speed_bonus")]
	public float SpeedBonus { get; set; } = 0.4f;
}
