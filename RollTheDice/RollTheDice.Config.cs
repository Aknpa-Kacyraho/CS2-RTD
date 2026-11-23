using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using RollTheDice.Configs;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice;

public partial class RollTheDice
{	private bool ReloadConfigFromDisk()
	{
		try
		{
			// Validate the exact file CSS will reload, with CSS's own JSON options.
			// JSON then applies through ConfigApplier so a throwing setter rolls back;
			// TOML keeps the CSS Reload path (we do not ship a TOML parser here).
			string path = Config.GetConfigPath();
			if (File.Exists(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			{
				string json = File.ReadAllText(path);
				if (!ConfigFileValidator.TryDeserialize(json, out PluginConfig? candidate, out string configError) || candidate == null)
				{
					throw new InvalidDataException(configError);
				}
				ConfigApplier.Apply(Config, candidate);
			}
			else
			{
				PluginConfigExtensions.Reload<PluginConfig>(Config);
			}
			// Defensive defaults: a hand-edited "dices": null / "maps": null must not
			// leave the plugin in a half-initialized state later in the map load.
			Config.Dices ??= new DicesConfig();
			Config.MapConfigs ??= new Dictionary<string, MapConfig>();
			Config.PlayerConfigs ??= new Dictionary<ulong, PlayerConfig>();
			DiceEffects.Enabled = Config.Effects?.Enabled ?? true;
			DiceEffects.TrailsEnabled = Config.Effects?.Trails ?? true;
			UpdatePlayerConfig();
			// Deliberately no Update() here: reloading must not write the in-memory copy
			// back to disk, otherwise a manual edit is overwritten on the next map start.
			return true;
		}
		catch (Exception ex)
		{
			string text = ((BasePlugin)this).Localizer["core.error"].Value.Replace("{error}", ex.Message);
			Console.WriteLine(text);
			Server.PrintToChatAll(text);
			return false;
		}
	}

	/// <summary>特效表路径：&lt;css&gt;/configs/plugins/RollTheDice/dicefx.json。</summary>
	private string FxDiceFxPath()
	{
		DirectoryInfo pluginDir = new DirectoryInfo(ModuleDirectory);
		string cssRoot = pluginDir.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
		return Path.Combine(cssRoot, "configs", "plugins", Path.GetFileName(ModuleDirectory), "dicefx.json");
	}

	/// <summary>重载 dicefx.json（fail-soft：失败保留上一份内存表）。</summary>
	private void ReloadFxTable()
	{
		try
		{
			List<string> known = new List<string>(_dices.Count);
			foreach (DiceBlueprint dice in _dices)
			{
				known.Add(dice.ClassName);
			}
			if (DiceFxTable.Reload(FxDiceFxPath(), known))
			{
				DiceEffects.ResetPersistent();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[DiceFX] reload failed: " + ex.Message);
		}
	}

	/// <summary>
	/// Fallback overlay used only when the raw JSON cannot be read (missing file or a
	/// runtime-only map config created by the admin command). Prefer
	/// <see cref="MapConfigMerger"/>: this path compares against default values and
	/// cannot distinguish an omitted field from an explicit default value.
	/// </summary>
	private static DicesConfig MergeMapDices(DicesConfig global, DicesConfig map)
	{
		DicesConfig merged = JsonSerializer.Deserialize<DicesConfig>(JsonSerializer.Serialize(global)) ?? global;
		foreach (PropertyInfo property in typeof(DicesConfig).GetProperties())
		{
			// Rarity is a shared block, not a per-dice config; a map default would undo
			// the global tier weights.
			if (property.Name == "Rarity")
			{
				continue;
			}
			object? mapDice = property.GetValue(map);
			if (mapDice == null)
			{
				continue;
			}
			object? mergedDice = property.GetValue(merged);
			if (mergedDice == null)
			{
				property.SetValue(merged, mapDice);
				continue;
			}
			object? defaultDice = Activator.CreateInstance(mergedDice.GetType());
			if (defaultDice == null)
			{
				continue;
			}
			foreach (PropertyInfo field in mergedDice.GetType().GetProperties())
			{
				// Only merge scalar overrides. Complex objects (e.g. RarityConfig) would
				// be replaced by their map default and undo global tuning.
				if (field.PropertyType.IsClass && field.PropertyType != typeof(string))
				{
					continue;
				}
				object? mapValue = field.GetValue(mapDice);
				if (mapValue == null)
				{
					continue;
				}
				object? defaultValue = field.GetValue(defaultDice);
				if (Equals(mapValue, defaultValue))
				{
					continue;
				}
				field.SetValue(mergedDice, mapValue);
			}
		}
		return merged;
	}

	private void LoadMapConfig(string mapName)
	{
		DicesConfig globalDices = Config.Dices ?? new DicesConfig();
		MapConfig? matched = null;
		string? matchedPattern = null;
		if (Config.MapConfigs != null)
		{
			foreach (KeyValuePair<string, MapConfig> entry in Config.MapConfigs)
			{
				if (FileSystemName.MatchesSimpleExpression(entry.Key.AsSpan(), mapName.AsSpan()))
				{
					matched = entry.Value;
					matchedPattern = entry.Key;
					break;
				}
			}
		}

		if (matched == null)
		{
			_currentMapConfig = new MapConfig { Dices = globalDices };
		}
		else
		{
			// Prefer the raw JSON overlay: it preserves which fields the map actually
			// wrote, including explicit false/0/default values. Fall back to the old
			// model-based merge only when the file cannot be read (e.g. a runtime-only
			// map config created by the admin command).
			JsonObject? mapDicesJson = matchedPattern == null ? null : ReadMapDicesJson(matchedPattern);
			DicesConfig mergedDices = mapDicesJson != null
				? MapConfigMerger.Merge(globalDices, mapDicesJson)
				: MergeMapDices(globalDices, matched.Dices ?? new DicesConfig());

			// Do not write the merged overlay back into matched.Dices: matched is the
			// deserialized map entry and must stay a pure overlay, otherwise the next
			// map load starts from a copy that already contains stale global values.
			_currentMapConfig = new MapConfig
			{
				Enabled = matched.Enabled,
				Dices = mergedDices
			};
		}
		if (_currentMapConfig.Dices == null)
		{
			_currentMapConfig.Dices = globalDices;
		}
		Console.WriteLine(((BasePlugin)this).Localizer["core.mapconfig"].Value.Replace("{mapName}", mapName));
	}

	/// <summary>
	/// Reads maps.&lt;pattern&gt;.dices from the on-disk JSON. Returns null on any
	/// failure or when the map has no dice block; the caller then falls back to the
	/// deserialized model merge. Never throws.
	/// </summary>
	private JsonObject? ReadMapDicesJson(string mapPattern)
	{
		try
		{
			string path = Config.GetConfigPath();
			if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
			{
				return null;
			}
			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
			if (!document.RootElement.TryGetProperty("maps", out JsonElement maps) ||
				maps.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			if (!maps.TryGetProperty(mapPattern, out JsonElement mapConfig) ||
				mapConfig.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			if (!mapConfig.TryGetProperty("dices", out JsonElement dices) ||
				dices.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			return JsonNode.Parse(dices.GetRawText()) as JsonObject;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[RollTheDice] failed to read raw map config for '" + mapPattern + "': " + ex.Message);
			return null;
		}
	}

	public void OnConfigParsed(PluginConfig config)
	{
		Config = config;
		// Keep the config graph usable even if a hand-edited file contains null blocks.
		Config.Dices ??= new DicesConfig();
		Config.MapConfigs ??= new Dictionary<string, MapConfig>();
		Config.PlayerConfigs ??= new Dictionary<ulong, PlayerConfig>();
		DiceEffects.Enabled = config.Effects?.Enabled ?? true;
		DiceEffects.TrailsEnabled = config.Effects?.Trails ?? true;
		Console.WriteLine(((BasePlugin)this).Localizer["core.config"]);
	}

	private void UpdatePlayerConfig()
	{
		Config.PlayerConfigs ??= new Dictionary<ulong, PlayerConfig>();
		_playerConfigs ??= new Dictionary<ulong, PlayerConfig>();
		if (_playerConfigs.Count == 0)
		{
			_playerConfigs = Config.PlayerConfigs;
			return;
		}
		if (ReferenceEquals(Config.PlayerConfigs, _playerConfigs))
		{
			return;
		}
		// Merge by SteamID without overwriting entries that were reloaded from disk:
		// only configs added at runtime are carried over, so a manual edit wins.
		foreach (KeyValuePair<ulong, PlayerConfig> entry in _playerConfigs)
		{
			if (!Config.PlayerConfigs.ContainsKey(entry.Key))
			{
				Config.PlayerConfigs[entry.Key] = entry.Value;
			}
		}
		_playerConfigs = Config.PlayerConfigs;
	}
}
