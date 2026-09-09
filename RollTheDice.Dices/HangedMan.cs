using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class HangedMan : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextTickTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, bool> _reversed = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "HangedMan";

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

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

	public HangedMan(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_nextTickTime[player] = 0f;
			_reversed[player] = false;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83d\udc80 命悬一线！每秒扣血，击杀敌人逆转诅咒！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_nextTickTime.Remove(player);
		_reversed.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_nextTickTime.Clear();
		_reversed.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker == (CEntityInstance)(object)@event.Userid)
		{
			return (HookResult)0;
		}
		if (_reversed.TryGetValue(attacker, out var value) & value)
		{
			return (HookResult)0;
		}
		_reversed[attacker] = true;
		attacker.PrintToCenterAlert("\ud83d\udd13 诅咒逆转！每秒恢复HP！");
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_HangedMan_broken"].Value.Replace("{playerName}", ((CBasePlayerController)attacker).PlayerName));
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_nextTickTime.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0 || !_nextTickTime.TryGetValue(item, out var value) || value > num)
				{
					continue;
				}
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				if (_reversed.TryGetValue(item, out var value3) & value3)
				{
					int healHp = _config.Dices.HangedMan.HealHp;
					int num2 = Math.Min(((CBaseEntity)value2).Health + healHp, ((CBaseEntity)value2).MaxHealth);
					if (num2 > ((CBaseEntity)value2).Health)
					{
						((CBaseEntity)value2).Health = num2;
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
					}
				}
				else
				{
					int drainHp = _config.Dices.HangedMan.DrainHp;
					((CBaseEntity)value2).Health -= drainHp;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
					if (((CBaseEntity)value2).Health <= 0 && !item.IsBot)
					{
						((CBasePlayerPawn)value2).CommitSuicide(false, true);
					}
				}
				_nextTickTime[item] = num + 1f;
			}
			catch
			{
				_nextTickTime.Remove(item);
			}
		}
	}
}
