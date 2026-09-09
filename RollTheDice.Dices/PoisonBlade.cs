using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class PoisonBlade : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, (int Ticks, int Dmg, float NextTime)> _poisoned = new Dictionary<CCSPlayerController, (int, int, float)>();

	public override string ClassName => "PoisonBlade";

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

	public PoisonBlade(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "IceBeam");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "霜毒双刃", "霜毒双刃联动生效！");
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
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_poisoned.Clear();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid || !_players.Contains(attacker) || (CEntityInstance)(object)userid.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)userid.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		float num = _config.Dices.PoisonBlade.ChanceMin + (float)_random.NextDouble() * (_config.Dices.PoisonBlade.ChanceMax - _config.Dices.PoisonBlade.ChanceMin);
		if (_random.NextDouble() >= (double)num)
		{
			return (HookResult)0;
		}
		int num2 = _random.Next(_config.Dices.PoisonBlade.DamagePerTickMin, _config.Dices.PoisonBlade.DamagePerTickMax + 1);
		int num3 = (DiceSynergy.HasPartner(attacker, "IceBeam") ? (_config.Dices.PoisonBlade.TickCount + 3) : _config.Dices.PoisonBlade.TickCount);
		float tickInterval = _config.Dices.PoisonBlade.TickInterval;
		_poisoned[userid] = (num3, num2, Server.CurrentTime);
		userid.PrintToCenterAlert($"☠ 中毒! {num3 * num2} 伤害 ({num2}x{num3})!");
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_poisoned.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		List<CCSPlayerController> list = new List<CCSPlayerController>();
		foreach (var (val2, tuple2) in _poisoned)
		{
			if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val2.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val2.PlayerPawn.Value).IsValid || ((CBaseEntity)val2.PlayerPawn.Value).LifeState != 0)
			{
				list.Add(val2);
			}
			else
			{
				if (num < tuple2.Item3)
				{
					continue;
				}
				(int, int, float) tuple3 = tuple2;
				int item = tuple3.Item1;
				int item2 = tuple3.Item2;
				float item3 = tuple3.Item3;
				CCSPlayerPawn value = val2.PlayerPawn.Value;
				((CBaseEntity)value).Health -= item2;
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				if (item <= 1 || ((CBaseEntity)value).Health <= 0)
				{
					if (((CBaseEntity)value).Health <= 0)
					{
						if (!val2.IsBot && !((CBasePlayerController)val2).IsHLTV)
						{
							((CBasePlayerPawn)value).CommitSuicide(false, true);
						}
						else
						{
							try
							{
								((CBasePlayerPawn)value).CommitSuicide(false, true);
							}
							catch
							{
								((CBaseEntity)value).Health = 0;
								Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
							}
						}
					}
					list.Add(val2);
				}
				else
				{
					_poisoned[val2] = (item - 1, item2, num + _config.Dices.PoisonBlade.TickInterval);
				}
			}
		}
		foreach (CCSPlayerController item4 in list)
		{
			_poisoned.Remove(item4);
		}
	}
}
