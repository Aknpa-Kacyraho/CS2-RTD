using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class ResetOnReloadConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("refill_on_kill")]
	public bool RefillOnKill { get; set; } = true;

	[JsonPropertyName("refill_on_reload")]
	public bool RefillOnReload { get; set; } = true;
}
