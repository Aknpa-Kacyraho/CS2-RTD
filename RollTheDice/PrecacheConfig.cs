using System.Text.Json.Serialization;

namespace RollTheDice;

public class PrecacheConfig
{
	[JsonPropertyName("soundevent_file")]
	public string SoundEventFile { get; set; } = "soundevents/soundevents_rollthedice.vsndevts";
}
