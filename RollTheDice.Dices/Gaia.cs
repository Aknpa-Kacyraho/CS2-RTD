using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Gaia : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextHealTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Gaia";

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

	public Gaia(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_nextHealTime[player] = 0f;
			_originalMaxHealth[player] = ((CBaseEntity)player.PlayerPawn.Value).MaxHealth;
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
		_nextHealTime.Remove(player);
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid && _originalMaxHealth.TryGetValue(player, out int origMax))
		{
			CCSPlayerPawn pawn = player.PlayerPawn?.Value;
			if ((CEntityInstance)(object)pawn != (CEntityInstance)null && ((CEntityInstance)pawn).IsValid)
			{
				((CBaseEntity)pawn).MaxHealth = origMax;
				if (((CBaseEntity)pawn).Health > origMax)
				{
					((CBaseEntity)pawn).Health = origMax;
				}
				Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iHealth", 0);
			}
			_originalMaxHealth.Remove(player);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_nextHealTime.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0 && (!_nextHealTime.TryGetValue(item, out var value) || num >= value))
			{
				_nextHealTime[item] = num + 1f;
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				int maxHP = _config.Dices.Gaia.MaxHP;
				((CBaseEntity)value2).MaxHealth = Math.Max(((CBaseEntity)value2).MaxHealth, Math.Min(((CBaseEntity)value2).Health + _config.Dices.Gaia.HpPerSecond, maxHP));
				((CBaseEntity)value2).Health = Math.Min(((CBaseEntity)value2).Health + _config.Dices.Gaia.HpPerSecond, maxHP);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}
}
