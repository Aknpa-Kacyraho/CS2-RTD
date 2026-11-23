using System;
using System.Text.Json;

namespace RollTheDice;

/// <summary>
/// Typed pre-validation for RollTheDice.json. JSON syntax alone is not enough:
/// CSS's Reload mutates the live config object, so a string where a bool is
/// expected can leave a half-applied config. Deserialize a throwaway copy first
/// with the same permissive shape CSS accepts (comments, trailing commas,
/// case-insensitive names) and only then let CSS reload.
/// </summary>
public static class ConfigFileValidator
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

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
}
