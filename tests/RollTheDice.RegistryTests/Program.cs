using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
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
		ConfigAndCommandChecks();
		MovementChecks();
		ComboChecks(registry);
		BroadcastChecks(built, localizer);
		ConfigApplyChecks();
		CleanupChecks(localizer);

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
		Check("entity dice cleanup overrides", new[]
		{
			typeof(Fate),
			typeof(Excalibur),
			typeof(FinalJudgment),
			typeof(MagneticPulse),
			typeof(Necromancer),
			typeof(ReverseCausality)
		}.All(type => type.GetMethod("OnHolderRemoved")?.DeclaringType == type));
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
		Check("amber ulong state included", amberFields.Contains("_frozenUntil"), string.Join(",", amberFields));
		Check("dictionary cleanup cast", new Dictionary<string, int>() is IDictionary);
	}

	private static void ConfigAndCommandChecks()
	{
		Check("config validator accepts object", ConfigFileValidator.TryDeserialize("{}", out _, out _));
		Check("config validator accepts comments", ConfigFileValidator.TryDeserialize(
			"{\n  // comment\n  \"enabled\": true\n}", out _, out _));
		Check("config validator rejects trailing comma", !ConfigFileValidator.TryDeserialize(
			"{\"enabled\":true,}", out _, out string commaError) && commaError.Length > 0, commaError);
		Check("config validator rejects type", !ConfigFileValidator.TryDeserialize(
			"{\"enabled\":\"yes\"}", out _, out string typeError) && typeError.Length > 0, typeError);
		Check("config validator ignores unknown", ConfigFileValidator.TryDeserialize(
			"{\"enabled\":true,\"unknown_future_key\":1}", out _, out _));

		string? deployed = Environment.GetEnvironmentVariable("RTD_CONFIG_PATH");
		if (!string.IsNullOrWhiteSpace(deployed) && File.Exists(deployed))
		{
			Check("deployed config parses", ConfigFileValidator.TryDeserialize(
				File.ReadAllText(deployed), out _, out string deployedError), deployedError);
		}

		MethodInfo? givedice = typeof(global::RollTheDice.RollTheDice).GetMethod("CommandGiveDice");
		CommandHelperAttribute? helper = givedice?.GetCustomAttribute<CommandHelperAttribute>();
		Check("givedice accepts zero args", helper != null && helper.MinArgs == 0,
			helper == null ? "attribute missing" : helper.MinArgs.ToString());
	}

	private static void MovementChecks()
	{
		MoveScopeStack stack = new MoveScopeStack();
		Check("move stack empty", stack.Count == 0 && stack.CurrentTarget == null);

		Check("move stack push", stack.Push("Amber", 0) == 0
			&& stack.Push("Skyline", 7) == 7
			&& stack.Count == 2
			&& stack.Contains("Amber"), stack.CurrentTarget?.ToString() ?? "null");
		Check("move stack update keeps priority", stack.Push("Amber", 0) == 7
			&& stack.Count == 2, stack.CurrentTarget?.ToString() ?? "null");

		Check("move stack pop", stack.Remove("Skyline", out int? afterSkyline)
			&& afterSkyline == 0
			&& stack.Remove("Amber", out int? afterAmber)
			&& afterAmber == null
			&& stack.Count == 0);
		Check("move stack unknown", !stack.Remove("Missing", out int? unchanged)
			&& unchanged == null && stack.Count == 0);

		stack.Push("A", 2);
		stack.Push("B", 7);
		stack.Clear();
		Check("move stack clear", stack.Count == 0 && stack.CurrentTarget == null);

		Check("move locks start empty", MoveLockManager.ActiveLockCount == 0
			&& MoveLockManager.LockedPlayerCount == 0);

		foreach (string file in new[] { "Amber.cs", "Fate.cs", "Phoenix.cs", "Skyline.cs", "Void.cs" })
		{
			string? path = FindRepoFile(Path.Combine("RollTheDice.Dices", file));
			if (path == null)
			{
				Check("movement source " + file, false, "file not found");
				continue;
			}
			string source = File.ReadAllText(path);
			Check("no direct MoveType " + file, !source.Contains(".MoveType =", StringComparison.Ordinal));
		}
	}

	private static void ComboChecks(DiceRegistry registry)
	{
		FieldInfo? field = typeof(DiceSynergy).GetField("_combos", BindingFlags.NonPublic | BindingFlags.Static);
		List<(string A, string B, string ComboName)>? combos =
			field?.GetValue(null) as List<(string A, string B, string ComboName)>;
		Check("combo table readable", combos != null);
		if (combos == null)
		{
			return;
		}

		Check("combo count", combos.Count == 68, combos.Count.ToString());
		Check("combo no self pair", combos.All(combo =>
			!string.Equals(combo.A, combo.B, StringComparison.OrdinalIgnoreCase)));
		HashSet<string> pairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Check("combo no duplicate pair", combos.All(combo =>
		{
			string key = string.Compare(combo.A, combo.B, StringComparison.OrdinalIgnoreCase) <= 0
				? combo.A + "|" + combo.B
				: combo.B + "|" + combo.A;
			return pairs.Add(key);
		}));

		List<IGrouping<string, (string A, string B, string ComboName)>> duplicateNames = combos
			.GroupBy(combo => combo.ComboName)
			.Where(group => group.Count() > 1)
			.ToList();
		Check("combo display names unique", duplicateNames.Count == 0,
			string.Join(",", duplicateNames.Select(group => group.Key + "x" + group.Count())));

		Check("combo registry guards", !registry.Issues.Any(issue =>
			issue.Message.Contains("pairs", StringComparison.Ordinal)
			|| issue.Message.Contains("duplicate combo pair", StringComparison.Ordinal)
			|| issue.Message.Contains("no display name", StringComparison.Ordinal)));

		List<string> resolved = DiceSynergy.ResolveComboNames(new HashSet<string> { "Giant", "RoyalBarrier" });
		Check("combo resolution", resolved.Contains("钢铁要塞")
			&& !resolved.Contains("Giant")
			&& !resolved.Contains("RoyalBarrier"), string.Join(",", resolved));

		List<string> shared = DiceSynergy.ResolveComboNames(new HashSet<string> { "Adrenaline", "Berserker", "Overheat" });
		Check("combo shared dice", shared.Contains("狂暴血脉")
			&& shared.Contains("狂热")
			&& !shared.Contains("Adrenaline")
			&& !shared.Contains("Berserker")
			&& !shared.Contains("Overheat"), string.Join(",", shared));

		List<string> mixed = DiceSynergy.ResolveComboNames(new HashSet<string> { "Giant", "RoyalBarrier", "Fireball" });
		Check("combo keeps standalone dice", mixed.Contains("钢铁要塞")
			&& mixed.Contains("Fireball")
			&& mixed.IndexOf("钢铁要塞") < mixed.IndexOf("Fireball"), string.Join(",", mixed));

		Check("no dice set changed overrides", !DiceBlueprint.HasDiceSetChangedOverrides);

		string? path = FindRepoFile(Path.Combine("RollTheDice.Utils", "DiceSynergy.cs"));
		if (path == null)
		{
			Check("synergy source", false, "file not found");
			return;
		}
		string source = File.ReadAllText(path);
		Check("synergy avoids GetPlayers", !source.Contains("Utilities.GetPlayers", StringComparison.Ordinal));
		Check("synergy uses true name", source.Contains("GetTruePlayerName", StringComparison.Ordinal));
	}

	private static void BroadcastChecks(IReadOnlyList<DiceBlueprint> built, IStringLocalizer localizer)
	{
		string encoded = DiceUi.HtmlEncode("<b>&\"'");
		Check("broadcast html encode", encoded.Contains("&lt;b&gt;", StringComparison.Ordinal)
			&& encoded.Contains("&amp;", StringComparison.Ordinal)
			&& !encoded.Contains("<b>", StringComparison.Ordinal), encoded);

		DiceBlueprint? sample = built.FirstOrDefault(dice => dice.ClassName == "Awakener") ?? built.FirstOrDefault();
		if (sample == null)
		{
			Check("broadcast description", false, "no built dice");
			return;
		}
		string description = DiceUi.DiceDescription(sample, localizer);
		Check("broadcast description localized", description.Length > 0
			&& !description.Contains('{')
			&& !description.Contains('}')
			&& description != "dice_" + sample.ClassName + "_player", description);

		string? path = FindRepoFile(Path.Combine("RollTheDice", "RollTheDice.Announce.cs"));
		if (path == null)
		{
			Check("broadcast source", false, "file not found");
			return;
		}
		string source = File.ReadAllText(path);
		Check("broadcast source guards", source.Contains("DiceUi.DiceDescription", StringComparison.Ordinal)
			&& source.Contains("DiceUi.HtmlEncode", StringComparison.Ordinal)
			&& source.Contains("GetTruePlayerName(player)", StringComparison.Ordinal));

		string? mainPath = FindRepoFile(Path.Combine("RollTheDice", "RollTheDice.cs"));
		Check("broadcast add suppression", mainPath != null
			&& File.ReadAllText(mainPath).Contains("AddWithoutHolderNotification", StringComparison.Ordinal)
			&& !DiceBlueprint.HolderNotificationsSuppressed);

		string? blueprintPath = FindRepoFile(Path.Combine("RollTheDice.Dices", "DiceBlueprint.cs"));
		Check("broadcast suppression source", blueprintPath != null
			&& File.ReadAllText(blueprintPath).Contains("HolderNotificationsSuppressed", StringComparison.Ordinal));
	}

	private static void ConfigApplyChecks()
	{
		ApplyProbe target = new ApplyProbe { A = 1, B = 2 };
		ApplyProbe source = new ApplyProbe { A = 10, B = 20 };
		ConfigApplier.Apply(target, source);
		Check("config apply copies", target.A == 10 && target.B == 20,
			target.A + "/" + target.B);

		ThrowingApplyProbe throwingTarget = new ThrowingApplyProbe { A = 1 };
		ThrowingApplyProbe throwingSource = new ThrowingApplyProbe { A = 99 };
		bool threw = false;
		try
		{
			ConfigApplier.Apply(throwingTarget, throwingSource);
		}
		catch (InvalidOperationException)
		{
			threw = true;
		}
		Check("config apply rolls back", threw && throwingTarget.A == 1,
			threw + "/" + throwingTarget.A);

		PluginConfig live = new PluginConfig();
		bool parsed = ConfigFileValidator.TryDeserialize("{\"enabled\":false}", out PluginConfig? candidate, out _);
		Check("config apply parses json", parsed && candidate != null);
		if (candidate != null)
		{
			ConfigApplier.Apply(live, candidate);
		}
		Check("config apply json value", live.Enabled == false, live.Enabled.ToString());

		PluginConfig tomlLive = new PluginConfig();
		bool tomlParsed = ConfigFileValidator.TryDeserializeToml("enabled = false\n", out PluginConfig? tomlCandidate, out string tomlError);
		Check("config apply parses toml", tomlParsed && tomlCandidate != null, tomlError);
		if (tomlCandidate != null)
		{
			ConfigApplier.Apply(tomlLive, tomlCandidate);
		}
		Check("config apply toml value", tomlLive.Enabled == false, tomlLive.Enabled.ToString());
	}

	private sealed class ApplyProbe
	{
		public int A { get; set; }

		public int B { get; set; }
	}

	private sealed class ThrowingApplyProbe
	{
		public int A { get; set; }

		public int B
		{
			get => 0;
			set => throw new InvalidOperationException("boom");
		}
	}

	private static void CleanupChecks(IStringLocalizer localizer)
	{
		PluginConfig pluginConfig = new PluginConfig();
		MapConfig mapConfig = new MapConfig();
		CleanupProbeDice probe = new CleanupProbeDice(pluginConfig, mapConfig, localizer);
		probe.Primitive[123UL] = 1;
		probe.EntityLike[123UL] = new object();
		probe.Ids.Add(123UL);
		probe.CleanupHolderState(123UL, RollTheDice.Enums.DiceRemoveReason.Disconnect);

		Check("ulong cleanup primitive", !probe.Primitive.ContainsKey(123UL));
		Check("ulong cleanup set", !probe.Ids.Contains(123UL));
		Check("ulong cleanup skips entity values", probe.EntityLike.ContainsKey(123UL));
	}

	private sealed class CleanupProbeDice : DiceBlueprint
	{
		public readonly Dictionary<ulong, int> Primitive = new Dictionary<ulong, int>();

		public readonly Dictionary<ulong, object> EntityLike = new Dictionary<ulong, object>();

		public readonly HashSet<ulong> Ids = new HashSet<ulong>();

		public CleanupProbeDice(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
			: base(globalConfig, config, localizer)
		{
		}
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
