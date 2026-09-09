using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Goddess : DiceBlueprint
{
	public static bool ActiveThisRound = false;

	public static readonly HashSet<ulong> BlessedPlayers = new HashSet<ulong>();

	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Goddess";

	public override List<string> Events => new List<string>();

	public override List<string> Listeners => new List<string>();

	public Goddess(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		ActiveThisRound = true;
		_comboActive = DiceSynergy.HasPartner(player, "God");
		if (_comboActive)
		{
			DiceSynergy.AnnounceCombo(player, "神之共鸣", "上帝伤害翻倍+女神多祝福一人！");
		}
		int count = (DiceSynergy.HasPartner(player, "God") ? 3 : 2);
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p != (CEntityInstance)(object)player && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0
			select p).ToList();
		for (int num = list.Count - 1; num > 0; num--)
		{
			int num2 = _random.Next(num + 1);
			List<CCSPlayerController> list2 = list;
			int index = num;
			int index2 = num2;
			CCSPlayerController value = list[num2];
			CCSPlayerController value2 = list[num];
			list2[index] = value;
			list[index2] = value2;
		}
		List<CCSPlayerController> list3 = list.Take(count).ToList();
		foreach (CCSPlayerController item in list3)
		{
			BlessedPlayers.Add(((CBasePlayerController)item).SteamID);
		}
		string value3 = _localizer["dice_Goddess_player"].Value;
		foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid
			select p)
		{
			item2.PrintToChat(_localizer["command.prefix"].Value + value3);
		}
		if (list3.Count > 0)
		{
			string newValue = string.Join(", ", list3.Select((CCSPlayerController p) => ((CBasePlayerController)p).PlayerName));
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Goddess_blessed"].Value.Replace("{names}", newValue));
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if (_players.Count == 0 && reason != DiceRemoveReason.NewDice)
		{
			ActiveThisRound = false;
		}
	}

	public override void Reset()
	{
		_players.Clear();
		ActiveThisRound = false;
		BlessedPlayers.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}
}
