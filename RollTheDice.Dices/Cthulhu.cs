using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Cthulhu : DiceBlueprint
{
	private bool _comboActive;

	private static float _roundStartTime;

	private static int _cthulhuTeam;

	private readonly Dictionary<ulong, float> _enemySpeedReduction = new Dictionary<ulong, float>();

	private int _lastWarningSecond = -1;

	public override string ClassName => "Cthulhu";

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

	public Cthulhu(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseEntity)value).MaxHealth = 1;
			((CBaseEntity)value).Health = 1;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			MoveLockManager.Lock(player, "Cthulhu");
			_players.Add(player);
			if (_roundStartTime == 0f)
			{
				_roundStartTime = Server.CurrentTime;
			}
			_cthulhuTeam = ((CBaseEntity)player).TeamNum;
			_lastWarningSecond = -1;
			_comboActive = DiceSynergy.HasPartner(player, "DuskDawn");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "深渊觉醒", "克苏恩+暮光！深渊觉醒了！");
				DuskDawn.TriggerDawn(player);
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc19 克苏恩降临！{_config.Dices.Cthulhu.KillTime:F0}秒后吞噬一切！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		MoveLockManager.Unlock(player, "Cthulhu");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			MoveLockManager.Unlock(item, "Cthulhu");
		}
		_players.Clear();
		_roundStartTime = 0f;
		_cthulhuTeam = 0;
		_enemySpeedReduction.Clear();
		_lastWarningSecond = -1;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_roundStartTime == 0f)
		{
			return;
		}
		float num = Server.CurrentTime;
		float num2 = num - _roundStartTime;
		float killTime = _config.Dices.Cthulhu.KillTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				MoveLockManager.Lock(item, "Cthulhu");
			}
			if (DiceSynergy.HasPartner(item, "DuskDawn") && Server.TickCount % 64 == 0 && (CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value = item.PlayerPawn.Value;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health + 1, ((CBaseEntity)value).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			}
		}
		int num3 = (int)Math.Ceiling(killTime - num2);
		if (num3 != _lastWarningSecond)
		{
			_lastWarningSecond = num3;
			if (num3 == 30 || num3 == 10 || num3 == 5)
			{
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc19 克苏恩还剩{num3}秒！");
			}
		}
		if (_players.Count > 0)
		{
			foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p)
			{
				if (((CBaseEntity)item2).TeamNum == _cthulhuTeam)
				{
					continue;
				}
				CCSPlayerPawn value2 = item2.PlayerPawn.Value;
				ulong steamID = ((CBasePlayerController)item2).SteamID;
				float valueOrDefault = _enemySpeedReduction.GetValueOrDefault(steamID, 0f);
				float num4 = valueOrDefault + _config.Dices.Cthulhu.SpeedLossPerSec * Server.TickInterval;
				if (num4 > 1f)
				{
					num4 = 1f;
				}
				_enemySpeedReduction[steamID] = num4;
				value2.VelocityModifier = 1f - num4;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				int num5 = _config.Dices.Cthulhu.HpLossPerSec * ((!_players.Any((CCSPlayerController p) => ((CBaseEntity)p).TeamNum == _cthulhuTeam && DiceSynergy.HasPartner(p, "DuskDawn"))) ? 1 : 2);
				if (Server.TickCount % 64 != 0)
				{
					continue;
				}
				((CBaseEntity)value2).Health -= num5;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				if (((CBaseEntity)value2).Health > 0)
				{
					continue;
				}
				if (!item2.IsBot && !((CBasePlayerController)item2).IsHLTV)
				{
					((CBasePlayerPawn)value2).CommitSuicide(false, true);
					continue;
				}
				try
				{
					((CBasePlayerPawn)value2).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)value2).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				}
			}
		}
		if (!(num2 >= killTime))
		{
			return;
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udc19 克苏恩吞噬万物！所有敌人被吞噬！");
		foreach (CCSPlayerController item3 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			if (((CBaseEntity)item3).TeamNum == _cthulhuTeam)
			{
				continue;
			}
			if (!item3.IsBot && !((CBasePlayerController)item3).IsHLTV)
			{
				((CBasePlayerPawn)item3.PlayerPawn.Value).CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerPawn)item3.PlayerPawn.Value).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)item3.PlayerPawn.Value).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)item3.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
			}
		}
		_roundStartTime = 0f;
	}
}
