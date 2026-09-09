using System.Text.Json.Serialization;
using RollTheDice.Enums;

namespace RollTheDice;

public class TriggerConfig
{
	[JsonPropertyName("event")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public DiceTriggerEvent TriggerEvent { get; set; } = DiceTriggerEvent.RoundStart;

	[JsonPropertyName("force_all_players")]
	public bool ForceAllPlayers { get; set; } = true;

	[JsonPropertyName("allow_player_auto_rtd")]
	public bool AllowPlayerAutoRtd { get; set; } = true;

	[JsonPropertyName("roll_the_dice_every_x_seconds")]
	public int RollTheDiceEveryXSeconds { get; set; } = 0;
}
