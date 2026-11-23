using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice;

public partial class RollTheDice
{	[ConsoleCommand("rtd_health", "RollTheDice runtime health summary")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "")]
	public void CommandHealth(CCSPlayerController player, CommandInfo command)
	{
		int holders = _dices.Sum(d => d._players.Count);
		string timers = $"auto={(_autoRollTimer != null ? 1 : 0)} reminder={(_roundReminderTimer != null ? 1 : 0)} name={_playerNames.RefreshTimerCount}";
		string registry = _diceRegistry == null
			? "none"
			: _diceRegistry.ErrorCount == 0 && _diceRegistry.WarningCount == 0
				? "ok"
				: $"E{_diceRegistry.ErrorCount}/W{_diceRegistry.WarningCount}";
		string[] lines =
		{
			$"[RTD] v{ModuleVersion} | map={_currentMap} | enabled={Config?.Enabled} | effects={(DiceEffects.Enabled ? "on" : "off")}",
			$"  dices={_dices.Count} | holders={holders} | round={_isDuringRound} | roundActive={DiceBlueprint.RoundActive}",
			$"  fx={DiceFxTable.Count} | timers[{timers}] | move_locks={MoveLockManager.ActiveLockCount}keys/{MoveLockManager.LockedPlayerCount}p",
			_diceRegistry == null
				? "  registry=none"
				: $"  registry={registry} | {_diceRegistry.Summary}"
		};
		foreach (string line in lines)
		{
			command.ReplyToCommand(line);
			Console.WriteLine(line);
		}
	}

	/// <summary>All dice currently held by a player (by SteamID).</summary>
	private List<DiceBlueprint> GetDiceOf(CCSPlayerController player)
	{
		ulong steamId = player.SteamID;
		List<DiceBlueprint> held = new List<DiceBlueprint>();
		foreach (DiceBlueprint dice in _dices)
		{
			if (dice.HasPlayer(steamId))
			{
				held.Add(dice);
			}
		}
		return held;
	}


	private void PrintRollHelp(CCSPlayerController player, CommandInfo command)
	{
		foreach (string key in new[]
		{
			"command.rtd.help.title",
			"command.rtd.help.roll",
			"command.rtd.help.auto",
			"command.rtd.help.status",
			"command.rtd.help.list",
			"command.rtd.help.tab",
			"command.rtd.help.skins",
			"command.rtd.help.tip"
		})
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer[key]);
		}

		try
		{
			if (AdminManager.PlayerHasPermissions(player, new[] { "@rollthedice/admin" }))
			{
				command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.rtd.help.admin"]);
			}
		}
		catch
		{
			// Permission lookup is best-effort; the player help must still print.
		}
	}

	private void PrintRollStatus(CCSPlayerController player, CommandInfo command)
	{
		List<DiceBlueprint> held = GetDiceOf(player);
		if (held.Count == 0)
		{
			command.ReplyToCommand(((BasePlugin)this).Localizer["command.rtd.status.none"].Value);
		}
		else
		{
			command.ReplyToCommand(((BasePlugin)this).Localizer["command.rtd.status.title.count"].Value
				.Replace("{count}", held.Count.ToString()));
			foreach (DiceBlueprint dice in held
				.OrderByDescending(dice => DiceUi.TierRank(GetDiceTier(dice)))
				.ThenBy(dice => DiceUi.DiceName(dice, ((BasePlugin)this).Localizer), StringComparer.OrdinalIgnoreCase))
			{
				string tier = GetDiceTier(dice);
				command.ReplyToCommand(DiceUi.DiceLine(dice, ((BasePlugin)this).Localizer, tier));
			}
		}

		bool hasConfig = _playerConfigs.TryGetValue(player.SteamID, out PlayerConfig playerConfig);
		bool autoRoll = hasConfig && playerConfig.RtdOnSpawn;
		bool showTab = !hasConfig || playerConfig.ShowDiceInTab;
		command.ReplyToCommand(((BasePlugin)this).Localizer[autoRoll
			? "command.rtd.status.auto.on"
			: "command.rtd.status.auto.off"].Value);
		command.ReplyToCommand(((BasePlugin)this).Localizer[showTab
			? "command.rtd.status.tab.on"
			: "command.rtd.status.tab.off"].Value);

		string cooldown = ((BasePlugin)this).Localizer["command.rtd.status.ready"].Value;
		if (_PlayerCooldown.TryGetValue(player, out int cooldownValue))
		{
			if (Config.CooldownRounds > 0 && cooldownValue > 0)
			{
				cooldown = ((BasePlugin)this).Localizer["command.rtd.status.cooldown.rounds"].Value
					.Replace("{rounds}", cooldownValue.ToString());
			}
			else if (Config.CooldownSeconds > 0)
			{
				int remain = cooldownValue - (int)Server.CurrentTime;
				if (remain > 0)
				{
					cooldown = ((BasePlugin)this).Localizer["command.rtd.status.cooldown.seconds"].Value
						.Replace("{seconds}", remain.ToString());
				}
			}
		}
		command.ReplyToCommand(cooldown);
		command.ReplyToCommand(((BasePlugin)this).Localizer["command.rtd.status.rolls"].Value
			.Replace("{count}", GetDiceRollCount(player).ToString())
			.Replace("{max}", GetMaxDiceCount(player).ToString()));
		command.ReplyToCommand(((BasePlugin)this).Localizer["command.rtd.status.next"].Value);
	}

	/// <summary>
	/// Read-only dice catalog. Three modes: overview, tier listing and keyword search.
	/// It never rolls, changes cooldowns or touches holder state.
	/// </summary>
	private void PrintDiceList(CCSPlayerController player, CommandInfo command, string filter)
	{
		var localizer = ((BasePlugin)this).Localizer;
		string? tierFilter = DiceUi.NormalizeTier(filter);
		if (string.IsNullOrWhiteSpace(filter))
		{
			Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (DiceBlueprint dice in _dices)
			{
				string tier = GetDiceTier(dice);
				counts.TryGetValue(tier, out int count);
				counts[tier] = count + 1;
			}
			command.ReplyToCommand(localizer["command.rtd.list.title"].Value
				.Replace("{count}", _dices.Count.ToString()));
			command.ReplyToCommand(localizer["command.rtd.list.tiers"].Value
				.Replace("{legendary}", TierCount(counts, "legendary").ToString())
				.Replace("{combo}", TierCount(counts, "combo").ToString())
				.Replace("{epic}", TierCount(counts, "epic").ToString())
				.Replace("{rare}", TierCount(counts, "rare").ToString())
				.Replace("{common}", TierCount(counts, "common").ToString()));
			command.ReplyToCommand(localizer["command.rtd.list.usage"].Value);
			return;
		}

		if (tierFilter != null)
		{
			List<DiceBlueprint> tierDice = _dices
				.Where(dice => GetDiceTier(dice) == tierFilter)
				.OrderBy(dice => DiceUi.DiceName(dice, localizer), StringComparer.OrdinalIgnoreCase)
				.ToList();
			command.ReplyToCommand(localizer["command.rtd.list.tier"].Value
				.Replace("{tier}", DiceUi.TierLabel(tierFilter))
				.Replace("{count}", tierDice.Count.ToString()));
			const int maxNames = 24;
			IEnumerable<string> names = tierDice
				.Take(maxNames)
				.Select(dice => DiceUi.DiceNameMarked(dice, localizer, dice.HasPlayer(player.SteamID)));
			foreach (string line in DiceUi.Columns(names, 4))
			{
				command.ReplyToCommand(line);
			}
			if (tierDice.Count > maxNames)
			{
				command.ReplyToCommand(localizer["command.rtd.list.truncated"].Value
					.Replace("{shown}", maxNames.ToString())
					.Replace("{more}", (tierDice.Count - maxNames).ToString()));
			}
			return;
		}

		List<DiceBlueprint> matches = _dices
			.Where(dice => DiceUi.MatchesFilter(dice, localizer, filter))
			.OrderByDescending(dice => DiceUi.TierRank(GetDiceTier(dice)))
			.ThenBy(dice => DiceUi.DiceName(dice, localizer), StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (matches.Count == 0)
		{
			command.ReplyToCommand(localizer["command.rtd.list.none"].Value.Replace("{query}", filter));
			return;
		}
		const int maxDetail = 8;
		foreach (DiceBlueprint dice in matches.Take(maxDetail))
		{
			command.ReplyToCommand(DiceUi.DiceLine(dice, localizer, GetDiceTier(dice)));
		}
		if (matches.Count > maxDetail)
		{
			command.ReplyToCommand(localizer["command.rtd.list.truncated"].Value
				.Replace("{shown}", maxDetail.ToString())
				.Replace("{more}", (matches.Count - maxDetail).ToString()));
		}
	}

	private static int TierCount(IReadOnlyDictionary<string, int> counts, string tier)
	{
		return counts.TryGetValue(tier, out int count) ? count : 0;
	}
}
