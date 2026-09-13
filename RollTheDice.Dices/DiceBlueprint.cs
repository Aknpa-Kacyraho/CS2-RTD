using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class DiceBlueprint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
{
	public readonly PluginConfig _globalConfig = GlobalConfig;

	public readonly MapConfig _config = Config;

	public readonly IStringLocalizer _localizer = Localizer;

	public readonly List<CCSPlayerController> _players = new List<CCSPlayerController>();

	public virtual string Description { get; private set; } = "Unknown Dice";

	public virtual string ClassName => "DiceBlueprint";

	public virtual bool CanBeDrawn => true;

	public virtual float Weight
	{
		get
		{
			var rarity = _globalConfig?.Dices?.Rarity;
			if (rarity != null)
			{
				if (rarity.DiceTier.TryGetValue(ClassName, out string tier) && tier != null && rarity.TierWeights.TryGetValue(tier, out float configured))
				{
					return configured;
				}
				if (rarity.TierWeights.TryGetValue("common", out float common))
				{
					return common;
				}
			}
			return 1f;
		}
	}

	public virtual bool IsSpecial => false;

	public virtual float SecondRoundProbability => 0f;

	public virtual string? SecondRoundRewardId => null;

	public virtual bool RequiresDrawSimulation => false;

	public virtual List<string> ExcludeFromReroll => new List<string>();

	public virtual string? TeammateBonusDice => null;

	public virtual float TeammateBonusChance => 0f;

	public virtual List<string> Events => new List<string>();

	public virtual List<string> Listeners => new List<string>();

	public virtual Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode>();

	public virtual List<string> Precache => new List<string>();

	public virtual float GetCooldownRemaining(CCSPlayerController player)
	{
		return 0f;
	}

	public virtual void OnDiceSetChanged(CCSPlayerController player)
	{
	}

	public virtual void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public virtual void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public virtual void Reset()
	{
		_players.Clear();
	}

	public virtual void Destroy()
	{
		Reset();
	}

	public void NotifyPlayers(CCSPlayerController player, string diceName, Dictionary<string, string> data)
	{
		if (_localizer["dice_" + diceName + "_player"].ResourceNotFound || (!_globalConfig.NotifyPlayerViaChatMsg && !_globalConfig.NotifyPlayerViaCenterMsg))
		{
			return;
		}
		string text = _localizer["dice_" + diceName + "_player"].Value;
		foreach (KeyValuePair<string, string> datum in data)
		{
			text = text.Replace("{" + datum.Key + "}", datum.Value);
		}
		if (_globalConfig.NotifyPlayerViaCenterMsg)
		{
			player.PrintToCenter(text);
		}
		if (_globalConfig.NotifyPlayerViaChatMsg)
		{
			player.PrintToChat(_localizer["command.prefix"].Value + text);
		}
		Description = text;
	}

	public void NotifyStatus(CCSPlayerController player, string diceName, Dictionary<string, string> data)
	{
		if (_localizer["dice_" + diceName + "_status"].ResourceNotFound)
		{
			return;
		}
		string text = _localizer["dice_" + diceName + "_status"].Value;
		foreach (KeyValuePair<string, string> datum in data)
		{
			text = text.Replace("{" + datum.Key + "}", datum.Value);
		}
		player.PrintToCenter(text);
		player.PrintToChat(_localizer["command.prefix"].Value + text);
		Description = text;
	}
}
