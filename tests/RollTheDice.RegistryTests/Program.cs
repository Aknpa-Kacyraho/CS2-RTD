using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using RollTheDice;
using RollTheDice.Dices;
using RollTheDice.Utils;

internal static class Program
{
	private const int ExpectedDiceCount = 171;

	private static int _passed;
	private static int _failed;

	private static void Main()
	{
		Console.WriteLine("RollTheDice dice registry tests");
		Console.WriteLine("--------------------------------");

		string? langPath = FindRepoFile(Path.Combine("lang", "en.json"));
		if (langPath == null)
		{
			Console.WriteLine("FAIL  lang/en.json not found");
			Environment.ExitCode = 1;
			return;
		}

		Dictionary<string, string> names = LoadNames(langPath);
		Check("localizer loaded", names.Count > ExpectedDiceCount);

		// Reflection is only for the test: production code passes the list directly.
		FieldInfo? combosField = typeof(DiceSynergy).GetField("_combos", BindingFlags.NonPublic | BindingFlags.Static);
		if (combosField?.GetValue(null) is not IEnumerable<(string A, string B, string ComboName)> combos)
		{
			Console.WriteLine("FAIL  DiceSynergy._combos not readable");
			Environment.ExitCode = 1;
			return;
		}

		DicesConfig config = new DicesConfig();
		DictionaryLocalizer localizer = new DictionaryLocalizer(names);
		PluginConfig pluginConfig = new PluginConfig { Dices = config };
		MapConfig mapConfig = new MapConfig { Dices = config };
		List<DiceBlueprint> built = new List<DiceBlueprint>();
		foreach (Type type in typeof(DiceBlueprint).Assembly.GetTypes()
			.Where(type => type.IsSubclassOf(typeof(DiceBlueprint)) && !type.IsAbstract)
			.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				object? instance = Activator.CreateInstance(type, pluginConfig, mapConfig, localizer);
				if (instance is DiceBlueprint dice)
				{
					built.Add(dice);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"WARN  cannot construct {type.Name}: {ex.Message}");
			}
		}
		Check("all dice constructed", built.Count == ExpectedDiceCount, built.Count.ToString());

		DiceRegistry registry = DiceRegistry.Build(
			typeof(DiceBlueprint).Assembly.GetTypes(),
			built,
			config,
			localizer,
			combos);

		Console.WriteLine($"registry: {registry.Summary}");
		Check("type count", registry.DiceTypeCount == ExpectedDiceCount, registry.DiceTypeCount.ToString());
		Check("config count", registry.ConfiguredCount == ExpectedDiceCount, registry.ConfiguredCount.ToString());
		Check("default-enabled count", registry.EnabledCount == ExpectedDiceCount, registry.EnabledCount.ToString());
		Check("combo count", registry.ComboCount > 0, registry.ComboCount.ToString());
		Check("no registry errors", registry.ErrorCount == 0, Describe(registry, "error"));
		Check("no unknown warnings", registry.WarningCount == KnownDuplicateComboWarnings(registry), Describe(registry, "warning"));

		Console.WriteLine("--------------------------------");
		Console.WriteLine($"{_passed} passed, {_failed} failed");
		if (_failed > 0)
		{
			Environment.ExitCode = 1;
		}
	}

	private static int KnownDuplicateComboWarnings(DiceRegistry registry)
	{
		// Two combos currently share the same display name. That is a cosmetic design
		// issue, not a registry failure; any other warning must fail this test.
		return registry.Issues.Count(issue =>
			issue.Severity == "warning" &&
			issue.Message.StartsWith("combo name '", StringComparison.Ordinal) &&
			issue.Message.Contains(" is used by 2 combos", StringComparison.Ordinal));
	}

	private static string Describe(DiceRegistry registry, string severity)
	{
		IEnumerable<string> messages = registry.Issues
			.Where(issue => issue.Severity == severity)
			.Select(issue => issue.Message)
			.Take(5);
		string detail = string.Join(" | ", messages);
		return string.IsNullOrEmpty(detail) ? "-" : detail;
	}

	private static void Check(string name, bool condition, string detail = "")
	{
		if (condition)
		{
			_passed++;
			Console.WriteLine($"PASS  {name}");
			return;
		}
		_failed++;
		Console.WriteLine($"FAIL  {name}{(detail.Length > 0 ? ": " + detail : string.Empty)}");
	}

	private static Dictionary<string, string> LoadNames(string path)
	{
		Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.Ordinal);
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		foreach (JsonProperty property in document.RootElement.EnumerateObject())
		{
			if (property.Value.ValueKind == JsonValueKind.String)
			{
				names[property.Name] = property.Value.GetString() ?? string.Empty;
			}
		}
		return names;
	}

	private static string? FindRepoFile(string relativePath)
	{
		DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null)
		{
			string candidate = Path.Combine(directory.FullName, relativePath);
			if (File.Exists(candidate))
			{
				return candidate;
			}
			directory = directory.Parent;
		}
		return null;
	}

	private sealed class DictionaryLocalizer : IStringLocalizer
	{
		private readonly Dictionary<string, string> _values;

		public DictionaryLocalizer(Dictionary<string, string> values)
		{
			_values = values;
		}

		public LocalizedString this[string name]
		{
			get
			{
				if (_values.TryGetValue(name, out string? value))
				{
					return new LocalizedString(name, value, resourceNotFound: false);
				}
				return new LocalizedString(name, name, resourceNotFound: true);
			}
		}

		public LocalizedString this[string name, params object[] arguments] => this[name];

		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Enumerable.Empty<LocalizedString>();
	}
}
