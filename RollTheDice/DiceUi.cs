using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Dices;

namespace RollTheDice;

/// <summary>
/// One place for the command-surface visual language: rarity labels/colors,
/// dice names/descriptions, dice lines and column layout. Dice gameplay code
/// does not depend on this class; it only formats what the commands print.
/// </summary>
public static class DiceUi
{
	public static string TierLabel(string tier)
	{
		return tier switch
		{
			"rare" => "稀有",
			"epic" => "史诗",
			"legendary" => "传说",
			"combo" => "组合",
			_ => "普通"
		};
	}

	/// <summary>Same color mapping as v1.4.5, expressed with ChatColors instead of raw escapes.</summary>
	public static char TierColor(string tier)
	{
		return tier switch
		{
			"rare" => ChatColors.Green,
			"epic" => ChatColors.LightPurple,
			"legendary" => ChatColors.Yellow,
			"combo" => ChatColors.DarkRed,
			_ => ChatColors.Default
		};
	}

	/// <summary>Higher first when the status panel sorts held dice.</summary>
	public static int TierRank(string tier)
	{
		return tier switch
		{
			"legendary" => 4,
			"combo" => 3,
			"epic" => 2,
			"rare" => 1,
			_ => 0
		};
	}

	/// <summary>Accept Chinese and English rarity names; null means "not a tier filter".</summary>
	public static string? NormalizeTier(string filter)
	{
		if (string.IsNullOrWhiteSpace(filter))
		{
			return null;
		}
		return filter.Trim().ToLowerInvariant() switch
		{
			"传说" or "legendary" or "legend" or "leg" => "legendary",
			"组合" or "combo" => "combo",
			"史诗" or "epic" => "epic",
			"稀有" or "rare" => "rare",
			"普通" or "common" => "common",
			_ => null
		};
	}

	public static bool MatchesFilter(DiceBlueprint dice, IStringLocalizer localizer, string filter)
	{
		if (string.IsNullOrWhiteSpace(filter))
		{
			return true;
		}
		string query = filter.Trim();
		return DiceName(dice, localizer).Contains(query, StringComparison.OrdinalIgnoreCase)
			|| dice.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	public static string DiceName(DiceBlueprint dice, IStringLocalizer localizer)
	{
		string key = "dice_" + dice.ClassName + "_name";
		string name = localizer[key];
		return name == key ? dice.ClassName : name;
	}

	public static string DiceDescription(DiceBlueprint dice, IStringLocalizer localizer)
	{
		string key = "dice_" + dice.ClassName + "_player";
		string description = localizer[key];
		if (description == key)
		{
			description = dice.Description ?? string.Empty;
		}
		return Regex.Replace(description ?? string.Empty, "\\{[^}]*\\}", string.Empty).Trim();
	}

	/// <summary>Encode dynamic text before inserting it into center HTML.</summary>
	public static string HtmlEncode(string? value)
	{
		return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
	}

	/// <summary>
	/// Admin list label: readable localized name plus the exact ClassName that
	/// `!givedice &lt;player&gt; &lt;dice&gt;` accepts. Plain class names stay unadorned.
	/// </summary>
	public static string DiceListLabel(DiceBlueprint dice, IStringLocalizer localizer)
	{
		string name = DiceName(dice, localizer);
		return name == dice.ClassName
			? name
			: $"{name} {ChatColors.Grey}({dice.ClassName}){ChatColors.Default}";
	}

	public static string DiceLine(DiceBlueprint dice, IStringLocalizer localizer, string tier)
	{
		string name = DiceName(dice, localizer);
		string label = TierLabel(tier);
		string description = DiceDescription(dice, localizer);
		string color = TierColor(tier).ToString();
		string head = $"  {color}●{ChatColors.Default} {name} [{label}]";
		return description.Length == 0 ? head : head + " — " + description;
	}

	/// <summary>Name plus a green check when the player currently holds the dice.</summary>
	public static string DiceNameMarked(DiceBlueprint dice, IStringLocalizer localizer, bool held)
	{
		string name = DiceName(dice, localizer);
		return held ? name + ChatColors.Green + "✓" + ChatColors.Default : name;
	}

	/// <summary>Pack items into chat lines of at most <paramref name="columns"/> entries.</summary>
	public static IEnumerable<string> Columns(IEnumerable<string> items, int columns)
	{
		if (columns < 1)
		{
			columns = 1;
		}
		List<string> batch = new List<string>(columns);
		foreach (string item in items)
		{
			batch.Add(item);
			if (batch.Count < columns)
			{
				continue;
			}
			yield return string.Join("  ", batch);
			batch.Clear();
		}
		if (batch.Count > 0)
		{
			yield return string.Join("  ", batch);
		}
	}
}
