using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Lottery : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Lottery";

	public Lottery(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		foreach (CCSPlayerController player2 in Utilities.GetPlayers())
		{
			if ((CEntityInstance)(object)player2 == (CEntityInstance)null || !((CEntityInstance)player2).IsValid || ((CBasePlayerController)player2).IsHLTV || player2.InGameMoneyServices == null)
			{
				continue;
			}
			int num = _random.Next(_config.Dices.Lottery.MoneyMin, _config.Dices.Lottery.MoneyMax + 1);
			if (num != 0)
			{
				if (num > 0)
				{
					player2.InGameMoneyServices.Account += num;
					player2.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Lottery_win"].Value.Replace("{amount}", num.ToString()));
				}
				else
				{
					int num2 = Math.Max(0, player2.InGameMoneyServices.Account + num);
					player2.InGameMoneyServices.Account = num2;
					player2.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Lottery_lose"].Value.Replace("{amount}", Math.Abs(num).ToString()));
				}
				Utilities.SetStateChanged((CBaseEntity)(object)player2, "CCSPlayerController", "m_pInGameMoneyServices", 0);
			}
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Lottery_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}
}
