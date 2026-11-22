using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
		string timers = $"auto={(_autoRollTimer != null ? 1 : 0)} reminder={(_roundReminderTimer != null ? 1 : 0)} name={_nameRefreshTimers.Count}";
		string summary = $"[RTD] v{ModuleVersion} | map={_currentMap} | enabled={Config?.Enabled} | dices={_dices.Count} | holders={holders} | round={_isDuringRound} | roundActive={DiceBlueprint.RoundActive} | fx={DiceFxTable.Count} | timers[{timers}] | effects={(DiceEffects.Enabled ? "on" : "off")}";
		command.ReplyToCommand(summary);
		Console.WriteLine(summary);
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

	private static string TierLabel(string tier)
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

	private static string TierColor(string tier)
	{
		return tier switch
		{
			"rare" => "\u0004",
			"epic" => "\u0003",
			"legendary" => "\u0009",
			"combo" => "\u0002",
			_ => "\u0001"
		};
	}

	private void PrintRollHelp(CCSPlayerController player, CommandInfo command)
	{
		foreach (string key in new[]
		{
			"command.rtd.help.title",
			"command.rtd.help.roll",
			"command.rtd.help.auto",
			"command.rtd.help.status",
			"command.rtd.help.tab",
			"command.rtd.help.skins"
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
			command.ReplyToCommand(((BasePlugin)this).Localizer["command.rtd.status.title"].Value);
			foreach (DiceBlueprint dice in held)
			{
				string nameKey = "dice_" + dice.ClassName + "_name";
				string name = ((BasePlugin)this).Localizer[nameKey];
				if (name == nameKey)
				{
					name = dice.ClassName;
				}
				string descriptionKey = "dice_" + dice.ClassName + "_player";
				string description = ((BasePlugin)this).Localizer[descriptionKey];
				if (description == descriptionKey)
				{
					description = dice.Description ?? string.Empty;
				}
				description = Regex.Replace(description, "\\{[^}]*\\}", string.Empty).Trim();
				string tier = GetDiceTier(dice);
				command.ReplyToCommand($"  {TierColor(tier)}●\u0001 {name} [{TierLabel(tier)}] — {description}");
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
}
