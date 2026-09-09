using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Pope : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, Dictionary<ulong, int>> _buffedTeammates = new Dictionary<CCSPlayerController, Dictionary<ulong, int>>();

	public override string ClassName => "Pope";

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

	public Pope(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Priest");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "神圣共鸣", "神圣共鸣联动生效！");
			}
			_buffedTeammates[player] = new Dictionary<ulong, int>();
			ApplyBuffToTeammates(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		RestoreTeammates(player);
		_players.Remove(player);
		_buffedTeammates.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			RestoreTeammates(item);
		}
		_players.Clear();
		_buffedTeammates.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid)
				{
					ApplyBuffToTeammates(item);
				}
			}
			catch
			{
			}
		}
	}

	private void ApplyBuffToTeammates(CCSPlayerController pope)
	{
		if (!_buffedTeammates.TryGetValue(pope, out Dictionary<ulong, int> value))
		{
			return;
		}
		int num = _config.Dices.Pope.HealthMultiplier + (DiceSynergy.HasPartner(pope, "Priest") ? 1 : 0);
		foreach (CCSPlayerController item in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			int result;
			if (((CEntityInstance)p).IsValid)
			{
				CHandle<CCSPlayerPawn> playerPawn = p.PlayerPawn;
				if (playerPawn != null)
				{
					CCSPlayerPawn value3 = playerPawn.Value;
					if (((value3 != null) ? new bool?(((CEntityInstance)value3).IsValid) : ((bool?)null)) == true && ((CBaseEntity)p).TeamNum == ((CBaseEntity)pope).TeamNum)
					{
						result = (((CEntityInstance)(object)p != (CEntityInstance)(object)pope) ? 1 : 0);
						goto IL_0061;
					}
				}
			}
			result = 0;
			goto IL_0061;
			IL_0061:
			return (byte)result != 0;
		}))
		{
			ulong steamID = ((CBasePlayerController)item).SteamID;
			if (!value.ContainsKey(steamID))
			{
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				int num2 = (value[steamID] = ((CBaseEntity)value2).MaxHealth);
				int num3 = num2 * num;
				((CBaseEntity)value2).MaxHealth = num3;
				((CBaseEntity)value2).Health = num3;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}

	private void RestoreTeammates(CCSPlayerController pope)
	{
		if (!_buffedTeammates.TryGetValue(pope, out Dictionary<ulong, int> value))
		{
			return;
		}
		foreach (KeyValuePair<ulong, int> item in value)
		{
			ulong sid = item.Key;
			int value2 = item.Value;
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == sid);
			if ((CEntityInstance)(object)((val == null) ? null : val.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)val.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value3 = val.PlayerPawn.Value;
				((CBaseEntity)value3).MaxHealth = value2;
				if (((CBaseEntity)value3).Health > value2)
				{
					((CBaseEntity)value3).Health = value2;
				}
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			}
		}
		value.Clear();
	}
}
