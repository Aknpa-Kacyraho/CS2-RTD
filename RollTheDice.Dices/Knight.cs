using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Knight : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextTransferTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Knight";

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

	public Knight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			((CBaseEntity)value).MaxHealth = _config.Dices.Knight.SelfMaxHealth;
			((CBaseEntity)value).Health = _config.Dices.Knight.SelfMaxHealth;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			_players.Add(player);
			_nextTransferTime[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_nextTransferTime.Remove(player);
		if (_originalMaxHealth.TryGetValue(player, out var value) && (CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value2 = player.PlayerPawn.Value;
			((CBaseEntity)value2).MaxHealth = value;
			if (((CBaseEntity)value2).Health > ((CBaseEntity)value2).MaxHealth)
			{
				((CBaseEntity)value2).Health = ((CBaseEntity)value2).MaxHealth;
			}
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
		}
		_originalMaxHealth.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_nextTransferTime.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		if (_nextTransferTime.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		float transferInterval = _config.Dices.Knight.TransferInterval;
		int hpTransfer = _config.Dices.Knight.HpTransfer;
		foreach (CCSPlayerController knight in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)knight == (CEntityInstance)null || !((CEntityInstance)knight).IsValid || (CEntityInstance)(object)knight.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)knight.PlayerPawn.Value).IsValid || ((CBaseEntity)knight.PlayerPawn.Value).LifeState != 0 || !_nextTransferTime.TryGetValue(knight, out var value) || value > num)
				{
					continue;
				}
				CCSPlayerPawn value2 = knight.PlayerPawn.Value;
				if (((CBaseEntity)value2).Health <= 1)
				{
					continue;
				}
				CsTeam knightTeam = knight.Team;
				int num2 = 0;
				foreach (CCSPlayerController item in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
				{
					//IL_001c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0022: Unknown result type (might be due to invalid IL or missing references)
					return (CEntityInstance)(object)p != (CEntityInstance)(object)knight && ((CEntityInstance)p).IsValid && p.Team == knightTeam && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && !_players.Contains(p);
				}))
				{
					if (((CBaseEntity)value2).Health <= 1)
					{
						break;
					}
					CCSPlayerPawn value3 = item.PlayerPawn.Value;
					if (((CBaseEntity)value3).Health < ((CBaseEntity)value3).MaxHealth)
					{
						int val = ((CBaseEntity)value3).MaxHealth - ((CBaseEntity)value3).Health;
						int val2 = Math.Min(hpTransfer, val);
						val2 = Math.Min(val2, ((CBaseEntity)value2).Health - 1);
						if (val2 > 0)
						{
							((CBaseEntity)value3).Health += val2;
							Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
							num2 += val2;
						}
					}
				}
				if (num2 > 0)
				{
					((CBaseEntity)value2).Health -= num2;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				}
				_nextTransferTime[knight] = num + transferInterval;
			}
			catch
			{
				_nextTransferTime.Remove(knight);
			}
		}
	}
}
