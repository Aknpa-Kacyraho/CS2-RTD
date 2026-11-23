using System;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Extensions;
using Tomlyn;

namespace RollTheDice;

/// <summary>
/// Typed pre-validation for RollTheDice.json. It deliberately uses the exact
/// serializer options CSS uses for PluginConfigExtensions.Reload, so validation
/// cannot accept a file that CSS will reject (or reject one CSS accepts).
/// CSS deserializes the whole file before copying properties, so the remaining
/// partial-update window is only a property setter throwing mid-copy.
/// </summary>
public static class ConfigFileValidator
{
	private static JsonSerializerOptions? _options;

	/// <summary>
	/// Prefer CSS's exact options. Outside a CSS host (tests/CI) ConfigManager's
	/// static initializer can fail, so fall back to the same values CSS uses today.
	/// </summary>
	private static JsonSerializerOptions Options
	{
		get
		{
			if (_options != null)
			{
				return _options;
			}
			try
			{
				_options = PluginConfigExtensions.JsonSerializerOptions;
			}
			catch
			{
				_options = new JsonSerializerOptions
				{
					WriteIndented = true,
					ReadCommentHandling = JsonCommentHandling.Skip
				};
			}
			return _options;
		}
	}

	public static bool TryDeserialize(string json, out PluginConfig? config, out string error)
	{
		try
		{
			config = JsonSerializer.Deserialize<PluginConfig>(json, Options);
			if (config == null)
			{
				error = "Config deserialized to null";
				return false;
			}
			error = string.Empty;
			return true;
		}
		catch (Exception ex)
		{
			config = null;
			error = ex.Message;
			return false;
		}
	}

	/// <summary>TOML counterpart so TOML reload can also use the atomic applier.</summary>
	public static bool TryDeserializeToml(string text, out PluginConfig? config, out string error)
	{
		try
		{
			config = Toml.ToModel<PluginConfig>(text, options: new TomlModelOptions
			{
				ConvertPropertyName = name => name
			});
			if (config == null)
			{
				error = "Config deserialized to null";
				return false;
			}
			error = string.Empty;
			return true;
		}
		catch (Exception ex)
		{
			config = null;
			error = ex.Message;
			return false;
		}
	}
}
