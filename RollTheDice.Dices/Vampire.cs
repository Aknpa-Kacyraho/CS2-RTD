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

public class Vampire : DiceBlueprint
{
	public readonly Random _random = new Random();

	public readonly Dictionary<CCSPlayerController, float> _playerSpeed = new Dictionary<CCSPlayerController, float>();

	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Vampire";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerHurt";
			return list;
		}
	}

	public Vampire(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			int maxHp = (int)float.Round(_config.Dices.Vampire.MaxHealth);
			((CBaseEntity)value).MaxHealth = maxHp;
			((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, maxHp);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			_comboActive = DiceSynergy.HasPartner(player, "SoulEater");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "噬魂血族", "HP上限" + maxHp + "！与噬魂者联动吸血翻倍！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (_originalMaxHealth.TryGetValue(player, out var value2))
			{
				((CBaseEntity)value).MaxHealth = value2;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				_originalMaxHealth.Remove(player);
			}
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || (CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(attacker) || (CEntityInstance)(object)attacker.PlayerPawn?.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		int num = (int)float.Round(@event.DmgHealth);
		if (DiceSynergy.HasPartner(attacker, "SoulEater"))
		{
			num *= 2;
		}
		CCSPlayerPawn val = attacker.PlayerPawn?.Value;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid)
		{
			return (HookResult)0;
		}
		int maxHp = (int)float.Round(_config.Dices.Vampire.MaxHealth);
		((CBaseEntity)val).Health = Math.Min(((CBaseEntity)val).Health + num, maxHp);
		Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iHealth", 0);
		attacker.PrintToCenterAlert($"+{num} HP!");
		return (HookResult)0;
	}
}
