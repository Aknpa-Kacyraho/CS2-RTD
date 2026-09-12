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

public class Jester : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextDamageTime = new Dictionary<CCSPlayerController, float>();

	private readonly HashSet<CCSPlayerController> _hasKill = new HashSet<CCSPlayerController>();

	public override string ClassName => "Jester";

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

	public Jester(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_nextDamageTime[player] = 0f;
			SpeedBonusManager.Register(player, "Jester", _config.Dices.Jester.SpeedMultiplier - 1f);
			DamageBonusManager.Register(player, "Jester", 0.2f);
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
		_nextDamageTime.Remove(player);
		_hasKill.Remove(player);
		SpeedBonusManager.Unregister(player, "Jester");
		DamageBonusManager.Unregister(player, "Jester");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			SpeedBonusManager.Unregister(item, "Jester");
			DamageBonusManager.Unregister(item, "Jester");
		}
		_players.Clear();
		_nextDamageTime.Clear();
		_hasKill.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid || ((CBaseEntity)userid).TeamNum == ((CBaseEntity)attacker).TeamNum || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		_hasKill.Add(attacker);
		attacker.PrintToCenterAlert("\ud83e\udd21 小丑笑了！移动不再扣血，改为回血！");
		return (HookResult)0;
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
			try
			{
				CCSPlayerPawn value2;
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0 && _nextDamageTime.TryGetValue(item, out var value) && !(value > num))
				{
					value2 = item.PlayerPawn.Value;
					float num2 = MathF.Sqrt(((CBaseEntity)value2).AbsVelocity.X * ((CBaseEntity)value2).AbsVelocity.X + ((CBaseEntity)value2).AbsVelocity.Y * ((CBaseEntity)value2).AbsVelocity.Y + ((CBaseEntity)value2).AbsVelocity.Z * ((CBaseEntity)value2).AbsVelocity.Z);
					if (!(num2 > 10f))
					{
						goto IL_01f1;
					}
					int health = ((CBaseEntity)value2).Health;
					if (_hasKill.Contains(item))
					{
						int num3 = Math.Min(health + _config.Dices.Jester.HealPerSecond, ((CBaseEntity)value2).MaxHealth);
						((CBaseEntity)value2).Health = num3;
						goto IL_01dd;
					}
					int num4 = health - _config.Dices.Jester.DamagePerSecond;
					if (num4 > 0)
					{
						((CBaseEntity)value2).Health = num4;
						goto IL_01dd;
					}
					if (!item.IsBot)
					{
						((CBasePlayerPawn)value2).CommitSuicide(false, true);
					}
				}
				goto end_IL_003f;
				IL_01dd:
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				goto IL_01f1;
				IL_01f1:
				float effective = SpeedBonusManager.GetEffective(item);
				value2.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				_nextDamageTime[item] = num + 1f;
				end_IL_003f:;
			}
			catch
			{
			}
		}
	}
}
