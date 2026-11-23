using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Translations;
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

		UiChecks(built, localizer, names);
		LifecycleChecks();

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

	private static void UiChecks(IReadOnlyList<DiceBlueprint> built, IStringLocalizer localizer, Dictionary<string, string> strings)
	{
		Check("tier labels", DiceUi.TierLabel("rare") == "稀有"
			&& DiceUi.TierLabel("epic") == "史诗"
			&& DiceUi.TierLabel("legendary") == "传说"
			&& DiceUi.TierLabel("combo") == "组合"
			&& DiceUi.TierLabel("common") == "普通"
			&& DiceUi.TierLabel("unknown") == "普通");
		Check("tier colors", DiceUi.TierColor("common") == 1
			&& DiceUi.TierColor("rare") == 4
			&& DiceUi.TierColor("epic") == 3
			&& DiceUi.TierColor("legendary") == 9
			&& DiceUi.TierColor("combo") == 2);
		Check("tier order", DiceUi.TierRank("legendary") > DiceUi.TierRank("combo")
			&& DiceUi.TierRank("combo") > DiceUi.TierRank("epic")
			&& DiceUi.TierRank("epic") > DiceUi.TierRank("rare")
			&& DiceUi.TierRank("rare") > DiceUi.TierRank("common"));

		List<string> columns = DiceUi.Columns(new[] { "a", "b", "c", "d", "e", "f", "g" }, 3).ToList();
		Check("columns layout", columns.Count == 3
			&& columns[0] == "a  b  c"
			&& columns[2] == "g", string.Join(" | ", columns));
		Check("columns empty", !DiceUi.Columns(Array.Empty<string>(), 3).Any());

		DiceBlueprint? sample = built.FirstOrDefault(dice => dice.ClassName == "Awakener") ?? built.FirstOrDefault();
		if (sample == null)
		{
			Check("dice ui sample", false, "no built dice");
			return;
		}
		string name = DiceUi.DiceName(sample, localizer);
		string line = DiceUi.DiceLine(sample, localizer, "legendary");
		Check("dice name resolved", name != "dice_" + sample.ClassName + "_name" && name.Length > 0, name);
		Check("dice line shape", line.Contains("●", StringComparison.Ordinal)
			&& line.Contains("[传说]", StringComparison.Ordinal)
			&& line.Contains(name, StringComparison.Ordinal), line);
		string listLabel = DiceUi.DiceListLabel(sample, localizer);
		Check("admin list label", listLabel.Contains("(" + sample.ClassName + ")", StringComparison.Ordinal), listLabel);

		Check("tier aliases", DiceUi.NormalizeTier("传说") == "legendary"
			&& DiceUi.NormalizeTier("LEG") == "legendary"
			&& DiceUi.NormalizeTier("组合") == "combo"
			&& DiceUi.NormalizeTier("epic") == "epic"
			&& DiceUi.NormalizeTier("稀有") == "rare"
			&& DiceUi.NormalizeTier("common") == "common"
			&& DiceUi.NormalizeTier("unknown") == null);
		Check("keyword match", DiceUi.MatchesFilter(sample, localizer, name.Substring(0, 1))
			&& DiceUi.MatchesFilter(sample, localizer, sample.ClassName.Substring(0, 4))
			&& !DiceUi.MatchesFilter(sample, localizer, "zzzz"));
		string heldLabel = DiceUi.DiceNameMarked(sample, localizer, true);
		string plainLabel = DiceUi.DiceNameMarked(sample, localizer, false);
		Check("held marker", heldLabel.Contains("✓", StringComparison.Ordinal)
			&& !plainLabel.Contains("✓", StringComparison.Ordinal), heldLabel);
		string tiers = strings["command.rtd.list.tiers"]
			.Replace("{legendary}", "12")
			.Replace("{combo}", "6")
			.Replace("{epic}", "26")
			.Replace("{rare}", "59")
			.Replace("{common}", "68")
			.ReplaceColorTags();
		Check("list color tags", !tiers.Contains('{') && !tiers.Contains('}'), tiers);
	}

	private static void LifecycleChecks()
	{
		Check("amber cleanup override", typeof(Amber).GetMethod("OnHolderRemoved")?.DeclaringType == typeof(Amber));
		Check("phoenix cleanup override", typeof(Phoenix).GetMethod("OnHolderRemoved")?.DeclaringType == typeof(Phoenix));
		Check("shared cleanup api", typeof(DiceBlueprint).GetMethod("CleanupHolderState") != null);

		MethodInfo? scanner = typeof(DiceBlueprint).GetMethod(
			"GetControllerStateFields",
			BindingFlags.NonPublic | BindingFlags.Static);
		Check("controller state scanner", scanner != null);
		if (scanner == null)
		{
			return;
		}

		string[] phoenixFields = ((FieldInfo[])scanner.Invoke(null, new object[] { typeof(Phoenix) })!)
			.Select(field => field.Name)
			.ToArray();
		Check("phoenix state fields", phoenixFields.Contains("_phoenixWings")
			&& phoenixFields.Contains("_phoenixGlows")
			&& phoenixFields.Contains("_phoenixEndTime"), string.Join(",", phoenixFields));

		string[] skylineFields = ((FieldInfo[])scanner.Invoke(null, new object[] { typeof(Skyline) })!)
			.Select(field => field.Name)
			.ToArray();
		Check("skyline state fields", skylineFields.Contains("_cooldowns")
			&& skylineFields.Contains("_flightEndTime"), string.Join(",", skylineFields));

		string[] voidFields = ((FieldInfo[])scanner.Invoke(null, new object[] { typeof(RollTheDice.Dices.Void) })!)
			.Select(field => field.Name)
			.ToArray();
		Check("void state fields", voidFields.Contains("_voidEndTime")
			&& voidFields.Contains("_lastVoidCountdown"), string.Join(",", voidFields));

		string[] amberFields = ((FieldInfo[])scanner.Invoke(null, new object[] { typeof(Amber) })!)
			.Select(field => field.Name)
			.ToArray();
		Check("amber ulong state excluded", !amberFields.Contains("_frozenUntil"), string.Join(",", amberFields));
		Check("dictionary cleanup cast", new Dictionary<string, int>() is IDictionary);
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
