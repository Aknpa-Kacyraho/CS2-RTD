using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using RollTheDice.Configs;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice;

public partial class RollTheDice
{	private void ReloadConfigFromDisk()
	{
		try
		{
			PluginConfigExtensions.Reload<PluginConfig>(Config);
			UpdatePlayerConfig();
			// Deliberately no Update() here: reloading must not write the in-memory copy
			// back to disk, otherwise a manual edit is overwritten on the next map start.
		}
		catch (Exception ex)
		{
			string text = ((BasePlugin)this).Localizer["core.error"].Value.Replace("{error}", ex.Message);
			Console.WriteLine(text);
			Server.PrintToChatAll(text);
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
	/// Map configs are overlays: start from the global dice config and copy only the
	/// values that differ from a default-constructed dice config. Without this, a map
	/// that disables one dice resets every other dice to defaults instead of globals.
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
		MapConfig? matched = (from mapConfig in Config.MapConfigs
			where FileSystemName.MatchesSimpleExpression(mapConfig.Key.AsSpan(), mapName.AsSpan())
			select mapConfig.Value).FirstOrDefault();
		if (matched == null)
		{
			_currentMapConfig = new MapConfig { Dices = Config.Dices };
		}
		else
		{
			matched.Dices = MergeMapDices(Config.Dices, matched.Dices ?? new DicesConfig());
			_currentMapConfig = matched;
		}
		if (_currentMapConfig.Dices == null)
		{
			_currentMapConfig.Dices = Config.Dices;
		}
		Console.WriteLine(((BasePlugin)this).Localizer["core.mapconfig"].Value.Replace("{mapName}", mapName));
	}

	public void OnConfigParsed(PluginConfig config)
	{
		Config = config;
		DiceEffects.Enabled = config.Effects?.Enabled ?? true;
		DiceEffects.TrailsEnabled = config.Effects?.Trails ?? true;
		Console.WriteLine(((BasePlugin)this).Localizer["core.config"]);
	}

	private void UpdatePlayerConfig()
	{
		if (_playerConfigs.Count == 0)
		{
			_playerConfigs = Config.PlayerConfigs;
		}
		else if (!ReferenceEquals(Config.PlayerConfigs, _playerConfigs))
		{
			// Merge by SteamID instead of replacing the whole dictionary, so entries
			// added externally while the plugin was running are not discarded.
			Config.PlayerConfigs ??= new Dictionary<ulong, PlayerConfig>();
			foreach (KeyValuePair<ulong, PlayerConfig> entry in _playerConfigs)
			{
				Config.PlayerConfigs[entry.Key] = entry.Value;
			}
		}
	}
}
