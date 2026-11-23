using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RollTheDice;

/// <summary>
/// Merges a map's raw JSON dice overlay into the global dice config by field
/// presence. This keeps explicit <c>false</c>, <c>0</c> and default-valued fields
/// meaningful, which the previous "compare against a default instance" merge could
/// not distinguish from an omitted field.
///
/// The existing <see cref="DicesConfig"/> model is intentionally left unchanged so
/// existing JSON files stay compatible; only the overlay path changes.
/// </summary>
public static class MapConfigMerger
{
	/// <summary>
	/// Returns a new config based on <paramref name="global"/> with every field that
	/// exists in <paramref name="mapDices"/> overlaid. Neither input is modified.
	/// </summary>
	public static DicesConfig Merge(DicesConfig global, JsonObject? mapDices)
	{
		DicesConfig baseline = global ?? new DicesConfig();
		if (mapDices == null || mapDices.Count == 0)
		{
			return Clone(baseline);
		}

		JsonNode? globalNode = JsonSerializer.SerializeToNode(baseline);
		if (globalNode is not JsonObject target)
		{
			return Clone(baseline);
		}

		Overlay(target, mapDices);
		try
		{
			return target.Deserialize<DicesConfig>() ?? Clone(baseline);
		}
		catch (Exception ex)
		{
			// A malformed overlay must not take the whole plugin down; the caller
			// falls back to the global config and logs the problem.
			Console.WriteLine("[RollTheDice] map overlay deserialize failed: " + ex.Message);
			return Clone(baseline);
		}
	}

	private static DicesConfig Clone(DicesConfig source)
	{
		return JsonSerializer.Deserialize<DicesConfig>(JsonSerializer.Serialize(source)) ?? new DicesConfig();
	}

	private static void Overlay(JsonObject target, JsonObject source)
	{
		foreach (var pair in source)
		{
			// Rarity is a shared block, not a per-dice scalar; maps do not override
			// it, matching the previous merge behaviour.
			if (pair.Key.Equals("rarity", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			JsonNode? sourceValue = pair.Value;
			if (sourceValue == null)
			{
				// The config model uses non-nullable fields; a JSON null means
				// "not set", so keep the global value instead of nulling it out.
				continue;
			}

			if (sourceValue is JsonObject sourceObject &&
				target.TryGetPropertyValue(pair.Key, out JsonNode? targetValue) &&
				targetValue is JsonObject targetObject)
			{
				Overlay(targetObject, sourceObject);
				continue;
			}

			// DeepCopy because the source node already belongs to the map overlay.
			target[pair.Key] = sourceValue.DeepClone();
		}
	}
}
