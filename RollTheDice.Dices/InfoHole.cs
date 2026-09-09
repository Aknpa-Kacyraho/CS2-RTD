using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class InfoHole : DiceBlueprint
{
	public override string ClassName => "InfoHole";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
			return list;
		}
	}

	public InfoHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			ApplyRadarState();
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if (_players.Count != 0)
		{
			return;
		}
		foreach (CCSPlayerController player2 in Utilities.GetPlayers())
		{
			if (!((CEntityInstance)(object)player2 == (CEntityInstance)null) && ((CEntityInstance)player2).IsValid && !((CBasePlayerController)player2).IsHLTV)
			{
				player2.ReplicateConVar("sv_disable_radar", "0");
			}
		}
	}

	public override void Reset()
	{
		try
		{
			foreach (CCSPlayerController player in Utilities.GetPlayers())
			{
				if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CBasePlayerController)player).IsHLTV)
				{
					player.ReplicateConVar("sv_disable_radar", "0");
				}
			}
		}
		catch
		{
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		try
		{
			Reset();
		}
		catch
		{
			_players.Clear();
		}
	}

	private void ApplyRadarState()
	{
		if (_players.Count == 0)
		{
			return;
		}
		CCSPlayerController val = _players[0];
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid)
		{
			return;
		}
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CBasePlayerController)player).IsHLTV)
			{
				if (((CBaseEntity)player).TeamNum != ((CBaseEntity)val).TeamNum)
				{
					player.ReplicateConVar("sv_disable_radar", "1");
				}
				else
				{
					player.ReplicateConVar("sv_disable_radar", "0");
				}
			}
		}
	}

	public void OnTick()
	{
		if (_players.Count != 0 && Server.TickCount % 128 == 0)
		{
			ApplyRadarState();
		}
	}
}
