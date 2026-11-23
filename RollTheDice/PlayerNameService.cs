using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice;

/// <summary>
/// Owns TAB-name state: original player names, the 1-second reapply timers and the
/// known dice/combo tag set. The main plugin only supplies read-only lookups.
/// </summary>
public sealed class PlayerNameService
{
	private readonly Dictionary<CCSPlayerController, string> _originalNames = new Dictionary<CCSPlayerController, string>();
	private readonly Dictionary<ulong, Timer> _refreshTimers = new Dictionary<ulong, Timer>();
	private readonly HashSet<string> _knownPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private Func<IReadOnlyList<DiceBlueprint>>? _getDices;
	private Func<string, DiceBlueprint?>? _getDiceByName;
	private Func<ulong, PlayerConfig?>? _getPlayerConfig;
	private IStringLocalizer? _localizer;
	private Func<float, Action, TimerFlags?, Timer>? _addTimer;
	private Action<string>? _logError;

	public int RefreshTimerCount => _refreshTimers.Count;

	public void Configure(
		Func<IReadOnlyList<DiceBlueprint>> getDices,
		Func<string, DiceBlueprint?> getDiceByName,
		Func<ulong, PlayerConfig?> getPlayerConfig,
		IStringLocalizer localizer,
		Func<float, Action, TimerFlags?, Timer> addTimer,
		Action<string> logError)
	{
		_getDices = getDices;
		_getDiceByName = getDiceByName;
		_getPlayerConfig = getPlayerConfig;
		_localizer = localizer;
		_addTimer = addTimer;
		_logError = logError;
	}

	/// <summary>Rebuild the tags this plugin is allowed to strip from TAB names.</summary>
	public void RebuildKnownPrefixes(
		IEnumerable<DiceBlueprint> dices,
		IStringLocalizer localizer,
		IEnumerable<string> comboNames)
	{
		_knownPrefixes.Clear();
		foreach (DiceBlueprint dice in dices)
		{
			_knownPrefixes.Add(dice.ClassName);
			string nameKey = "dice_" + dice.ClassName + "_name";
			string localizedName = localizer[nameKey];
			if (localizedName != nameKey)
			{
				_knownPrefixes.Add(localizedName);
			}
		}
		foreach (string comboName in comboNames)
		{
			_knownPrefixes.Add(comboName);
		}
	}

	public void ClearKnownPrefixes()
	{
		_knownPrefixes.Clear();
	}

	public void Refresh(CCSPlayerController player, bool scheduleReapply = true)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		try
		{
			ulong tabSteamId = player.SteamID;
			PlayerConfig? tabConfig = _getPlayerConfig?.Invoke(tabSteamId);
			if (tabConfig != null && !tabConfig.ShowDiceInTab)
			{
				string trueName = _originalNames.TryGetValue(player, out string cachedName)
					? cachedName
					: StripDiceNameDecoration(player.PlayerName);
				_originalNames[player] = trueName;
				player.PlayerName = trueName;
				Utilities.SetStateChanged((CBaseEntity)(object)player, "CBasePlayerController", "m_iszPlayerName", 0);
				return;
			}

			string text = ((CBasePlayerController)player).PlayerName;
			if (!_originalNames.TryGetValue(player, out string value))
			{
				// Strip only tags this plugin added; a blanket [anything] regex would
				// eat real [CLAN] prefixes.
				text = StripDiceNameDecoration(text);
				_originalNames[player] = text;
			}
			else
			{
				text = value;
			}

			HashSet<string> hashSet = new HashSet<string>();
			ulong refreshSteamId = player.SteamID;
			IReadOnlyList<DiceBlueprint> dices = _getDices?.Invoke() ?? Array.Empty<DiceBlueprint>();
			foreach (DiceBlueprint dice in dices)
			{
				if (!dice.HasPlayer(refreshSteamId))
				{
					continue;
				}
				string item = dice.ClassName == "Trickster" &&
					Trickster.PendingFakeNames.TryGetValue(((CBasePlayerController)player).SteamID, out string fakeName)
					? fakeName
					: dice.ClassName;
				hashSet.Add(item);
			}

			bool HasTeammateHolding(string className)
			{
				DiceBlueprint? partnerDice = _getDiceByName?.Invoke(className);
				if (partnerDice == null)
				{
					return false;
				}
				foreach (CCSPlayerController teammate in partnerDice._players)
				{
					if (teammate == null || !teammate.IsValid || teammate.IsHLTV)
					{
						continue;
					}
					if ((CEntityInstance)teammate == (CEntityInstance)player)
					{
						continue;
					}
					if (teammate.TeamNum == player.TeamNum)
					{
						return true;
					}
				}
				return false;
			}

			foreach (var combo in DiceSynergy._combos)
			{
				var (diceA, diceB, _) = combo;
				if (hashSet.Contains(diceA) && !hashSet.Contains(diceB) && HasTeammateHolding(diceB))
				{
					hashSet.Add(diceB);
				}
				if (hashSet.Contains(diceB) && !hashSet.Contains(diceA) && HasTeammateHolding(diceA))
				{
					hashSet.Add(diceA);
				}
			}

			IStringLocalizer? localizer = _localizer;
			if (localizer == null)
			{
				return;
			}
			List<string> list = DiceSynergy.ResolveComboNames(hashSet).Select(delegate (string comboName)
			{
				string key = "dice_" + comboName + "_name";
				string localized = localizer[key];
				return localized == key ? comboName : localized;
			}).ToList();
			string decorated = list.Count > 2
				? "[" + list[0] + "]+" + (list.Count - 1) + " " + text
				: list.Count <= 0
					? text
					: string.Join(" ", list.Select(n => "[" + n + "]")) + " " + text;
			((CBasePlayerController)player).PlayerName = decorated;
			Utilities.SetStateChanged((CBaseEntity)(object)player, "CBasePlayerController", "m_iszPlayerName", 0);

			if (!scheduleReapply)
			{
				return;
			}
			CCSPlayerController captured = player;
			ulong timerKey = player.SteamID;
			if (_refreshTimers.TryGetValue(timerKey, out Timer? previousTimer))
			{
				previousTimer.Kill();
				_refreshTimers.Remove(timerKey);
			}
			if (_addTimer == null)
			{
				return;
			}
			_refreshTimers[timerKey] = _addTimer(1f, delegate
			{
				_refreshTimers.Remove(timerKey);
				if (captured != null && captured.IsValid)
				{
					// Recompute instead of writing back the old string; old prefixes
					// otherwise survive round changes and keep growing.
					Refresh(captured, false);
				}
			}, TimerFlags.STOP_ON_MAPCHANGE);
		}
		catch (Exception ex)
		{
			_logError?.Invoke($"{DateTime.Now:HH:mm:ss} PlayerNameService.Refresh error for {player.PlayerName}: {ex.Message}\n");
		}
	}

	/// <summary>Restore cached real names on round end and clear the cache.</summary>
	public void RestoreAll()
	{
		foreach (KeyValuePair<CCSPlayerController, string> item in _originalNames.ToList())
		{
			if (item.Key != null && item.Key.IsValid)
			{
				item.Key.PlayerName = item.Value;
				Utilities.SetStateChanged((CBaseEntity)(object)item.Key, "CBasePlayerController", "m_iszPlayerName", 0);
			}
		}
		_originalNames.Clear();
	}

	public void StopRefreshTimers()
	{
		foreach (Timer timer in _refreshTimers.Values)
		{
			try
			{
				timer.Kill();
			}
			catch
			{
			}
		}
		_refreshTimers.Clear();
	}

	public void StopRefreshTimer(ulong steamId)
	{
		if (_refreshTimers.Remove(steamId, out Timer timer))
		{
			try
			{
				timer.Kill();
			}
			catch
			{
			}
		}
	}

	public void Forget(CCSPlayerController? player)
	{
		if (player != null)
		{
			_originalNames.Remove(player);
		}
	}

	public void ClearNames()
	{
		_originalNames.Clear();
	}

	public bool HasCachedName(CCSPlayerController player)
	{
		return _originalNames.ContainsKey(player);
	}

	/// <summary>Cached real name, falling back to the stripped current name.</summary>
	public string GetTrueName(CCSPlayerController player)
	{
		return _originalNames.TryGetValue(player, out string cached)
			? cached
			: StripDiceNameDecoration(player.PlayerName);
	}

	/// <summary>Cached real name, falling back to the current (possibly decorated) name.</summary>
	public string GetCachedOrCurrentName(CCSPlayerController player)
	{
		return _originalNames.TryGetValue(player, out string cached)
			? cached
			: player.PlayerName;
	}

	private string StripDiceNameDecoration(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name ?? string.Empty;
		}

		string result = name.Trim();
		bool changed = true;
		while (changed && result.Length > 0)
		{
			changed = false;
			foreach (string known in _knownPrefixes)
			{
				string tag = "[" + known + "]";
				if (!result.StartsWith(tag, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				result = result.Substring(tag.Length).TrimStart();
				if (result.StartsWith("+"))
				{
					int space = result.IndexOf(' ');
					if (space > 0)
					{
						result = result.Substring(space + 1).TrimStart();
					}
				}
				changed = true;
				break;
			}
		}
		return result.Length > 0 ? result : name.Trim();
	}
}
