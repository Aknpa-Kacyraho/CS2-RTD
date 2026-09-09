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

public class Lucky : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextTickTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _interval = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, (float EndTime, float Multiplier, string Type)> _activeBuffs = new Dictionary<CCSPlayerController, (float, float, string)>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Lucky";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public Lucky(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			float num = _config.Dices.Lucky.IntervalMin + (float)_random.NextDouble() * (_config.Dices.Lucky.IntervalMax - _config.Dices.Lucky.IntervalMin);
			_players.Add(player);
			_nextTickTime[player] = Server.CurrentTime + num;
			_interval[player] = num;
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
		_nextTickTime.Remove(player);
		_interval.Remove(player);
		_activeBuffs.Remove(player);
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				item.PlayerPawn.Value.VelocityModifier = 1f;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		_players.Clear();
		_nextTickTime.Clear();
		_interval.Clear();
		_activeBuffs.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CBaseEntity value = attacker.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn obj2 = ((NativeObject)value).As<CCSPlayerPawn>();
				if (obj2 == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj2).Controller;
					if (controller == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerController value2 = controller.Value;
						obj = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_activeBuffs.ContainsKey(val))
		{
			return (HookResult)0;
		}
		if (_activeBuffs.TryGetValue(val, out (float, float, string) value3) && value3.Item3 == "damage" && Server.CurrentTime < value3.Item1)
		{
			info.Damage *= value3.Item2;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		float now = Server.CurrentTime;
		List<CCSPlayerController> list = (from kv in _activeBuffs
			where now >= kv.Value.EndTime
			select kv.Key).ToList();
		foreach (CCSPlayerController item in list)
		{
			_activeBuffs.Remove(item);
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				item.PlayerPawn.Value.VelocityModifier = 1f;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		foreach (var (val2, tuple2) in _activeBuffs)
		{
			if (tuple2.Item3 == "speed" && (CEntityInstance)(object)((val2 == null) ? null : val2.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)val2.PlayerPawn.Value).IsValid)
			{
				val2.PlayerPawn.Value.VelocityModifier = tuple2.Item2;
				Utilities.SetStateChanged((CBaseEntity)(object)val2.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		if (_nextTickTime.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item2 in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item2 == (CEntityInstance)null || !((CEntityInstance)item2).IsValid || (CEntityInstance)(object)item2.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item2.PlayerPawn.Value).IsValid || ((CBaseEntity)item2.PlayerPawn.Value).LifeState != 0 || item2.InGameMoneyServices == null || !_nextTickTime.TryGetValue(item2, out var value) || value > now)
				{
					continue;
				}
				int num = _random.Next(_config.Dices.Lucky.MoneyMin, _config.Dices.Lucky.MoneyMax + 1);
				item2.InGameMoneyServices.Account += num;
				Utilities.SetStateChanged((CBaseEntity)(object)item2, "CCSPlayerController", "m_pInGameMoneyServices", 0);
				string value2 = "";
				switch (_random.Next(0, 4))
				{
				case 0:
					if (((CEntityInstance)item2.PlayerPawn.Value).IsValid)
					{
						item2.PlayerPawn.Value.VelocityModifier = 1.2f;
						Utilities.SetStateChanged((CBaseEntity)(object)item2.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
						_activeBuffs[item2] = (now + 5f, 1.2f, "speed");
					}
					value2 = "⚡加速";
					break;
				case 1:
					_activeBuffs[item2] = (now + 5f, 1.2f, "damage");
					value2 = "\ud83d\udcaa增伤";
					break;
				case 2:
				{
					CCSPlayerPawn value4 = item2.PlayerPawn.Value;
					((CBaseEntity)value4).Health = Math.Min(((CBaseEntity)value4).MaxHealth, ((CBaseEntity)value4).Health + 20);
					Utilities.SetStateChanged((CBaseEntity)(object)value4, "CBaseEntity", "m_iHealth", 0);
					value2 = "❤回血";
					break;
				}
				case 3:
				{
					CCSPlayerPawn value3 = item2.PlayerPawn.Value;
					value3.ArmorValue = Math.Min(100, value3.ArmorValue + 20);
					Utilities.SetStateChanged((CBaseEntity)(object)value3, "CCSPlayerPawn", "m_ArmorValue", 0);
					value2 = "\ud83d\udee1护甲";
					break;
				}
				}
				item2.PrintToCenterAlert($"\ud83c\udf40 幸运金币 +${num}! {value2}!");
				float num2 = (_interval.TryGetValue(item2, out var value5) ? value5 : (_config.Dices.Lucky.IntervalMin + (float)_random.NextDouble() * (_config.Dices.Lucky.IntervalMax - _config.Dices.Lucky.IntervalMin)));
				_nextTickTime[item2] = now + num2;
			}
			catch
			{
				_nextTickTime.Remove(item2);
				_interval.Remove(item2);
			}
		}
	}
}
