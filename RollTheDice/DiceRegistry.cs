using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice;

/// <summary>
/// Read-only inventory of dice metadata. Cross-checks code types, config entries,
/// rarity tiers, localization keys and combo references at startup. It never
/// changes draw weights, dice instances or config values; findings are only logged.
/// </summary>
public sealed class DiceRegistry
{
	public sealed class Issue
	{
		public Issue(string severity, string message)
		{
			Severity = severity;
			Message = message;
		}

		public string Severity { get; }

		public string Message { get; }
	}

	private readonly List<Issue> _issues = new List<Issue>();

	public int ErrorCount => _issues.Count(issue => issue.Severity == "error");

	public int WarningCount => _issues.Count(issue => issue.Severity == "warning");

	public IReadOnlyList<Issue> Issues => _issues;

	public int DiceTypeCount { get; private set; }

	public int ConfiguredCount { get; private set; }

	public int EnabledCount { get; private set; }

	public int ComboCount { get; private set; }

	public int MissingFxCount { get; private set; }

	public string Summary =>
		$"types={DiceTypeCount} configs={ConfiguredCount} enabled={EnabledCount} combos={ComboCount} errors={ErrorCount} warnings={WarningCount} fx_missing={MissingFxCount}";

	/// <summary>
	/// Build the registry from every non-abstract dice type, the dice instances that were
	/// actually enabled, and the read-only metadata sources. Never throws for a single bad
	/// entry: the caller is expected to wrap the whole build as a non-fatal step.
	/// </summary>
	public static DiceRegistry Build(
		IEnumerable<Type> diceTypes,
		IEnumerable<DiceBlueprint> builtDice,
		DicesConfig? config,
		IStringLocalizer localizer,
		IEnumerable<(string A, string B, string ComboName)> combos)
	{
		DiceRegistry registry = new DiceRegistry();
		List<Type> types = diceTypes
			.Where(type => type != null && type.IsSubclassOf(typeof(DiceBlueprint)) && !type.IsAbstract)
			.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
		registry.DiceTypeCount = types.Count;

		if (config == null)
		{
			registry.Add("error", "DicesConfig is null; dice/config validation skipped");
			return registry;
		}

		HashSet<string> typeNames = new HashSet<string>(types.Select(type => type.Name), StringComparer.OrdinalIgnoreCase);
		HashSet<string> enabledTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		PropertyInfo[] configProperties = typeof(DicesConfig).GetProperties();
		foreach (PropertyInfo property in configProperties)
		{
			if (property.Name == nameof(DicesConfig.Rarity))
			{
				continue;
			}
			object? value = property.GetValue(config);
			if (value == null)
			{
				registry.Add("warning", $"config.{property.Name} is null");
				continue;
			}
			registry.ConfiguredCount++;
			PropertyInfo? enabledProperty = value.GetType().GetProperty("Enabled");
			if (enabledProperty?.PropertyType != typeof(bool))
			{
				registry.Add("warning", $"config.{property.Name} has no bool Enabled; dice cannot be enabled");
			}
			else if (enabledProperty.GetValue(value) is bool enabled && enabled)
			{
				registry.EnabledCount++;
				enabledTypeNames.Add(property.Name);
			}
			if (!typeNames.Contains(property.Name))
			{
				registry.Add("warning", $"config.{property.Name} has no matching dice type");
			}
		}

		foreach (Type type in types)
		{
			PropertyInfo? property = configProperties.FirstOrDefault(candidate => candidate.Name == type.Name);
			if (property == null)
			{
				registry.Add("error", $"{type.Name} has no DicesConfig property; it can never be enabled");
				continue;
			}
			if (!HasBoolEnabled(property.GetValue(config)))
			{
				registry.Add("warning", $"{type.Name} config has no bool Enabled");
			}

			string nameKey = "dice_" + type.Name + "_name";
			LocalizedString localizedName = localizer[nameKey];
			if (localizedName.ResourceNotFound || localizedName.Value == nameKey)
			{
				registry.Add("warning", $"{type.Name} is missing localization key {nameKey}");
			}

			if (config.Rarity?.DiceTier == null || !config.Rarity.DiceTier.ContainsKey(type.Name))
			{
				registry.Add("warning", $"{type.Name} has no rarity tier (defaults to common)");
			}
			if (!DiceFxTable.TryGet(type.Name, out _))
			{
				registry.MissingFxCount++;
			}
		}

		registry.CheckBuiltDice(builtDice);
		registry.CheckEnabledInstances(enabledTypeNames, builtDice);
		registry.CheckCombos(combos, typeNames);
		return registry;
	}

	/// <summary>Write one summary line plus every error/warning. Info-only counters stay in the summary.</summary>
	public void Log(Action<string> write)
	{
		write($"[RTD] dice_registry: {Summary}");
		foreach (Issue issue in _issues)
		{
			write($"[RTD] dice_registry {issue.Severity}: {issue.Message}");
		}
	}

	private void CheckBuiltDice(IEnumerable<DiceBlueprint> builtDice)
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (DiceBlueprint dice in builtDice)
		{
			if (dice == null)
			{
				continue;
			}
			string className = dice.ClassName;
			if (!string.Equals(className, dice.GetType().Name, StringComparison.Ordinal))
			{
				Add("error", $"{dice.GetType().Name}.ClassName is '{className}'");
			}
			if (!seen.Add(className))
			{
				Add("error", $"duplicate ClassName '{className}'");
			}
		}
	}

	private void CheckEnabledInstances(
		HashSet<string> enabledTypeNames,
		IEnumerable<DiceBlueprint> builtDice)
	{
		HashSet<string> builtTypeNames = new HashSet<string>(
			builtDice.Select(dice => dice.GetType().Name),
			StringComparer.OrdinalIgnoreCase);
		foreach (string typeName in enabledTypeNames)
		{
			if (!builtTypeNames.Contains(typeName))
			{
				Add("error", $"{typeName} is enabled in config but was not instantiated");
			}
		}
	}

	private void CheckCombos(
		IEnumerable<(string A, string B, string ComboName)> combos,
		HashSet<string> typeNames)
	{
		Dictionary<string, int> nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var combo in combos)
		{
			ComboCount++;
			if (!typeNames.Contains(combo.A))
			{
				Add("warning", $"combo '{combo.ComboName}' references unknown dice {combo.A}");
			}
			if (!typeNames.Contains(combo.B))
			{
				Add("warning", $"combo '{combo.ComboName}' references unknown dice {combo.B}");
			}
			if (string.IsNullOrEmpty(combo.ComboName))
			{
				continue;
			}
			nameCounts.TryGetValue(combo.ComboName, out int count);
			nameCounts[combo.ComboName] = count + 1;
		}
		foreach (KeyValuePair<string, int> pair in nameCounts.Where(pair => pair.Value > 1))
		{
			Add("warning", $"combo name '{pair.Key}' is used by {pair.Value} combos");
		}
	}

	private void Add(string severity, string message)
	{
		_issues.Add(new Issue(severity, message));
	}

	private static bool HasBoolEnabled(object? value)
	{
		if (value == null)
		{
			return false;
		}
		PropertyInfo? property = value.GetType().GetProperty("Enabled");
		return property != null && property.PropertyType == typeof(bool);
	}
}
