using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice;

public partial class RollTheDice
{
	private void AnnounceDiceRarity(CCSPlayerController player, DiceBlueprint dice)
	{
		try
		{
			string tier = GetDiceTier(dice);
			string localized = DiceUi.DiceName(dice, ((BasePlugin)this).Localizer);
			string label = DiceUi.TierLabel(tier);
			string color = DiceUi.TierColor(tier).ToString();
			string prefix = ((BasePlugin)this).Localizer["command.prefix"].Value;
			string description = DiceUi.DiceDescription(dice, ((BasePlugin)this).Localizer);
			bool showHud = Config?.Effects?.Hud ?? true;
			player.PrintToChat($" {prefix}{color}🎲 你抽到了 [{label}] {localized} {((BasePlugin)this).Localizer["command.rtd.hint.status"].Value}");
			if (!showHud
				&& !string.IsNullOrEmpty(description)
				&& !description.StartsWith("Unknown", StringComparison.Ordinal))
			{
				// NotifyPlayers is suppressed during Add; keep the effect text in
				// chat when the HUD is disabled.
				player.PrintToChat($" {prefix}{description}");
			}
			if (showHud)
			{
				string effects = DiceEffects.DescribeEffects(dice.ClassName);
				string effectLine = string.IsNullOrEmpty(effects)
					? ""
					: $"<br/><span style=\"font-size:16px;color:#9ad\">特效：{DiceUi.HtmlEncode(effects)}</span>";
				string descriptionLine = string.IsNullOrEmpty(description) || description.StartsWith("Unknown", StringComparison.Ordinal)
					? ""
					: $"<br/><span style=\"font-size:16px;color:#ddd\">{DiceUi.HtmlEncode(description)}</span>";
				player.PrintToCenterHtml($"<div style=\"font-size:22px\"><b>🎲 {DiceUi.HtmlEncode(localized)}</b><br/><span style=\"font-size:16px;color:#fd6\">[{DiceUi.HtmlEncode(label)}]</span>{descriptionLine}{effectLine}</div>", 5);
			}
			if ((tier == "legendary" || tier == "combo") && (CurrentRarity?.BroadcastLegendary ?? true))
			{
				Server.PrintToChatAll($" {prefix}{color}🌟 {label}骰子降临！{GetTruePlayerName(player)} 抽到了【{localized}】！");
			}
			if ((tier == "legendary" || tier == "combo") && Config?.Sounds?.LegendarySounds != null && Config.Sounds.LegendarySounds.TryGetValue(dice.ClassName, out string legendarySound) && !string.IsNullOrEmpty(legendarySound))
			{
				BroadcastDiceSound(legendarySound);
			}
		}
		catch
		{
		}
	}

	/// <summary>把音效广播给所有在线真人（从各自身上发声 = 无距离衰减，人人听清）。</summary>
	private void BroadcastDiceSound(string sound)
	{
		try
		{
			if (string.IsNullOrEmpty(sound))
			{
				return;
			}
			float volume = Math.Clamp(Config.Sounds.Volume, 0f, 1f);
			foreach (CCSPlayerController target in Utilities.GetPlayers())
			{
				if (target == null || !((CEntityInstance)target).IsValid || target.IsBot || target.IsHLTV)
				{
					continue;
				}
				RecipientFilter filter = new RecipientFilter();
				filter.Add(target);
				((CBaseEntity)target).EmitSound(sound, filter, volume, 0f);
			}
		}
		catch
		{
		}
	}

}
