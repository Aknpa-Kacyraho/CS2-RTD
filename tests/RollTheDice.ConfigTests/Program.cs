using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using RollTheDice;

internal static class Program
{
	private const float Epsilon = 1e-5f;
	private static int _passed;
	private static int _failed;

	private static void Main()
	{
		Console.WriteLine("RollTheDice config merge tests");
		Console.WriteLine("--------------------------------");

		GlobalInheritedWhenMapHasNoDice();
		MapOverridesGlobal();
		MapExplicitFalse();
		MapExplicitZero();
		MapExplicitDefaultValue();
		MapMissingFieldInherits();
		ConsecutiveMapsDoNotLeak();
		ReloadGlobalChange();
		ReloadMapChange();
		RarityIsNotOverriddenByMap();
		JsonNullMeansNotSet();
		GlobalInstanceIsNotModified();
		LegacyMergeComparison();

		Console.WriteLine("--------------------------------");
		Console.WriteLine($"{_passed} passed, {_failed} failed");
		if (_failed > 0)
		{
			Environment.ExitCode = 1;
		}
	}

	private static void GlobalInheritedWhenMapHasNoDice()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = false;
		global.IncreaseSpeed.MaxBonus = 0.37f;

		var merged = MapConfigMerger.Merge(global, new JsonObject());

		Check("1a global enabled inherited", !merged.IncreaseSpeed.Enabled);
		Near("1b global max_bonus inherited", 0.37f, merged.IncreaseSpeed.MaxBonus);
		Near("1c default gain_per_second inherited", 0.1f, merged.IncreaseSpeed.GainPerSecond);
	}

	private static void MapOverridesGlobal()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = false;
		global.IncreaseSpeed.MaxBonus = 0.4f;
		var map = Parse("""{"increase_speed":{"enabled":true,"max_bonus":0.8}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Check("2a map enabled overrides global", merged.IncreaseSpeed.Enabled);
		Near("2b map max_bonus overrides global", 0.8f, merged.IncreaseSpeed.MaxBonus);
		Near("2c unmentioned gain_per_second inherited", 0.1f, merged.IncreaseSpeed.GainPerSecond);
	}

	private static void MapExplicitFalse()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = true;
		var map = Parse("""{"increase_speed":{"enabled":false}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Check("3 map explicit false wins", !merged.IncreaseSpeed.Enabled);
	}

	private static void MapExplicitZero()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.MaxBonus = 0.8f;
		var map = Parse("""{"increase_speed":{"max_bonus":0}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Near("4 map explicit zero wins", 0f, merged.IncreaseSpeed.MaxBonus);
	}

	private static void MapExplicitDefaultValue()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = false;
		// "enabled": true is the default value, but the map wrote it explicitly.
		var map = Parse("""{"increase_speed":{"enabled":true}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Check("5 map explicit default value wins", merged.IncreaseSpeed.Enabled);
	}

	private static void MapMissingFieldInherits()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = false;
		global.IncreaseSpeed.MaxBonus = 0.4f;
		var map = Parse("""{"increase_speed":{"max_bonus":0.9}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Check("6a missing enabled inherited", !merged.IncreaseSpeed.Enabled);
		Near("6b written max_bonus applied", 0.9f, merged.IncreaseSpeed.MaxBonus);
		Near("6c missing gain_per_second inherited", 0.1f, merged.IncreaseSpeed.GainPerSecond);
	}

	private static void ConsecutiveMapsDoNotLeak()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = true;
		global.IncreaseSpeed.MaxBonus = 0.5f;
		var mapA = Parse("""{"increase_speed":{"enabled":false,"max_bonus":0.9}}""");
		var mapB = Parse("""{"increase_speed":{"max_bonus":0.7}}""");

		var mergedA = MapConfigMerger.Merge(global, mapA);
		var mergedB = MapConfigMerger.Merge(global, mapB);

		Check("7a map A explicit false", !mergedA.IncreaseSpeed.Enabled);
		Near("7b map A max_bonus", 0.9f, mergedA.IncreaseSpeed.MaxBonus);
		Check("7c map B does not inherit map A false", mergedB.IncreaseSpeed.Enabled);
		Near("7d map B max_bonus", 0.7f, mergedB.IncreaseSpeed.MaxBonus);
		Check("7e global enabled unchanged", global.IncreaseSpeed.Enabled);
		Near("7f global max_bonus unchanged", 0.5f, global.IncreaseSpeed.MaxBonus);
	}

	private static void ReloadGlobalChange()
	{
		var map = Parse("""{"increase_speed":{"enabled":false}}""");
		var global1 = new DicesConfig();
		global1.IncreaseSpeed.MaxBonus = 0.5f;
		var merged1 = MapConfigMerger.Merge(global1, map);

		var global2 = new DicesConfig();
		global2.IncreaseSpeed.MaxBonus = 0.2f;
		var merged2 = MapConfigMerger.Merge(global2, map);

		Check("8a map false still wins after global reload", !merged1.IncreaseSpeed.Enabled && !merged2.IncreaseSpeed.Enabled);
		Near("8b first global max_bonus", 0.5f, merged1.IncreaseSpeed.MaxBonus);
		Near("8c reloaded global max_bonus", 0.2f, merged2.IncreaseSpeed.MaxBonus);
	}

	private static void ReloadMapChange()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.MaxBonus = 0.5f;
		var map1 = Parse("""{"increase_speed":{"max_bonus":0.9}}""");
		var map2 = Parse("""{"increase_speed":{"max_bonus":0.1}}""");

		var merged1 = MapConfigMerger.Merge(global, map1);
		var merged2 = MapConfigMerger.Merge(global, map2);

		Near("9a first map max_bonus", 0.9f, merged1.IncreaseSpeed.MaxBonus);
		Near("9b reloaded map max_bonus", 0.1f, merged2.IncreaseSpeed.MaxBonus);
	}

	private static void RarityIsNotOverriddenByMap()
	{
		var global = new DicesConfig();
		global.Rarity.TierWeights["common"] = 2f;
		var map = Parse("""{"rarity":{"tier_weights":{"common":99},"broadcast_legendary":false}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Near("10a rarity tier weight kept", 2f, merged.Rarity.TierWeights["common"]);
		Check("10b rarity broadcast kept", merged.Rarity.BroadcastLegendary);
	}

	private static void JsonNullMeansNotSet()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = false;
		global.IncreaseSpeed.MaxBonus = 0.4f;
		var map = Parse("""{"increase_speed":{"enabled":null,"max_bonus":0}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Check("11a null enabled keeps global", !merged.IncreaseSpeed.Enabled);
		Near("11b zero max_bonus still applies", 0f, merged.IncreaseSpeed.MaxBonus);
	}

	private static void GlobalInstanceIsNotModified()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = true;
		global.IncreaseSpeed.MaxBonus = 0.5f;
		var map = Parse("""{"increase_speed":{"enabled":false,"max_bonus":0.9}}""");

		var merged = MapConfigMerger.Merge(global, map);

		Check("12a merged is a different instance", !ReferenceEquals(global, merged));
		Check("12b global enabled untouched", global.IncreaseSpeed.Enabled);
		Near("12c global max_bonus untouched", 0.5f, global.IncreaseSpeed.MaxBonus);
	}

	private static void LegacyMergeComparison()
	{
		var global = new DicesConfig();
		global.IncreaseSpeed.Enabled = false;
		var map = Parse("""{"increase_speed":{"enabled":true}}""");
		var mapModel = map.Deserialize<DicesConfig>() ?? new DicesConfig();

		var legacy = LegacyMergeMapDices(global, mapModel);
		var current = MapConfigMerger.Merge(global, map);

		Check("13a legacy merge keeps global false (known limitation)", !legacy.IncreaseSpeed.Enabled);
		Check("13b new merge honours explicit default value", current.IncreaseSpeed.Enabled);
	}

	/// <summary>
	/// Copy of the pre-fix merge logic, kept only to document the behaviour change.
	/// </summary>
	private static DicesConfig LegacyMergeMapDices(DicesConfig global, DicesConfig map)
	{
		DicesConfig merged = JsonSerializer.Deserialize<DicesConfig>(JsonSerializer.Serialize(global)) ?? global;
		foreach (PropertyInfo property in typeof(DicesConfig).GetProperties())
		{
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

	private static JsonObject Parse(string json)
	{
		return JsonNode.Parse(json) as JsonObject
			?? throw new InvalidOperationException("test JSON is not an object: " + json);
	}

	private static void Check(string name, bool condition)
	{
		if (condition)
		{
			_passed++;
			Console.WriteLine("PASS  " + name);
			return;
		}
		_failed++;
		Console.WriteLine("FAIL  " + name);
	}

	private static void Near(string name, float expected, float actual)
	{
		Check(name + " (expected " + expected + ", actual " + actual + ")", Math.Abs(expected - actual) <= Epsilon);
	}
}
