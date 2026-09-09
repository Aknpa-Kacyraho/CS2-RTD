using System.Text.Json.Serialization;

namespace RollTheDice;

public class SoundConfig
{
	[JsonPropertyName("dice_sound")]
	public string DiceRollSound { get; set; } = "sounds/ui/coin_pickup_01.vsnd";

	[JsonPropertyName("play_on_command_only")]
	public bool PlayOnCommandOnly { get; set; } = false;

	[JsonPropertyName("volume")]
	public float Volume { get; set; } = 0.5f;
}
