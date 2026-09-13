using System.Collections.Generic;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using RollTheDice.Configs;

namespace RollTheDice;

public class PluginConfig : BasePluginConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("debug")]
	public bool Debug { get; set; } = false;

	[JsonPropertyName("allow_rtd_during_warmup")]
	public bool AllowRtdDuringWarmup { get; set; } = true;

	[JsonPropertyName("trigger")]
	public TriggerConfig DiceTrigger { get; set; } = new TriggerConfig();

	[JsonPropertyName("cooldown_rounds")]
	public int CooldownRounds { get; set; } = 0;

	[JsonPropertyName("cooldown_seconds")]
	public int CooldownSeconds { get; set; } = 0;

	[JsonPropertyName("price_to_dice")]
	public int PriceToDice { get; set; } = 0;

	[JsonPropertyName("allow_dice_after_respawn")]
	public bool AllowDiceAfterRespawn { get; set; } = false;

	[JsonPropertyName("notify_other_players_about_dices_rolled")]
	public bool NotifyOtherPlayers { get; set; } = true;

	[JsonPropertyName("notify_player_via_chatmsg")]
	public bool NotifyPlayerViaChatMsg { get; set; } = true;

	[JsonPropertyName("notify_player_via_centermsg")]
	public bool NotifyPlayerViaCenterMsg { get; set; } = true;

	[JsonPropertyName("dices")]
	public DicesConfig Dices { get; set; } = new DicesConfig();

	[JsonPropertyName("cheat_guard")]
	public CheatGuardConfig CheatGuard { get; set; } = new CheatGuardConfig();

	[JsonPropertyName("sounds")]
	public SoundConfig Sounds { get; set; } = new SoundConfig();

	[JsonPropertyName("precache")]
	public PrecacheConfig Precache { get; set; } = new PrecacheConfig();

	[JsonPropertyName("maps")]
	public Dictionary<string, MapConfig> MapConfigs { get; set; } = new Dictionary<string, MapConfig>();

	[JsonPropertyName("player_configs")]
	public Dictionary<ulong, PlayerConfig> PlayerConfigs { get; set; } = new Dictionary<ulong, PlayerConfig>();
}
