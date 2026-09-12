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

public class Overheat : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _stacks = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _nextTickTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Overheat";

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

	public Overheat(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_stacks[player] = 0;
			_nextTickTime[player] = Server.CurrentTime + _config.Dices.Overheat.Interval;
			bool flag = DiceSynergy.HasPartner(player, "Adrenaline");
			if (flag)
			{
				DiceSynergy.AnnounceCombo(player, "狂热", "速度获取翻倍，上限翻倍！");
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
		_stacks.Remove(player);
		_nextTickTime.Remove(player);
		DamageBonusManager.Unregister(player, "Overheat");
		SpeedBonusManager.Unregister(player, "Overheat");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageBonusManager.Unregister(item, "Overheat");
			SpeedBonusManager.Unregister(item, "Overheat");
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				item.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(item, 100f);
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		_players.Clear();
		_stacks.Clear();
		_nextTickTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private (float speedPerStack, float damagePerStack, int maxStacks) GetComboParams(CCSPlayerController player)
	{
		bool flag = DiceSynergy.HasPartner(player, "Adrenaline");
		float item = (flag ? (_config.Dices.Overheat.SpeedPerStack * 2f) : _config.Dices.Overheat.SpeedPerStack);
		float item2 = (flag ? (_config.Dices.Overheat.DamagePerStack * 2f) : _config.Dices.Overheat.DamagePerStack);
		int item3 = (flag ? (_config.Dices.Overheat.MaxStacks * 2) : _config.Dices.Overheat.MaxStacks);
		return (speedPerStack: item, damagePerStack: item2, maxStacks: item3);
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)attacker == (CEntityInstance)(object)@event.Userid)
		{
			return (HookResult)0;
		}
		if (_stacks.TryGetValue(attacker, out var value) && value > 0)
		{
			int num = Math.Max(1, (int)((float)_config.Dices.Overheat.MaxStacks * 0.1f));
			_stacks[attacker] = Math.Max(0, value - num);
			if ((CEntityInstance)(object)attacker.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)attacker.PlayerPawn.Value).IsValid)
			{
				float item = GetComboParams(attacker).speedPerStack;
				SpeedBonusManager.Register(attacker, "Overheat", (float)_stacks[attacker] * item);
				float num2 = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
				attacker.PlayerPawn.Value.VelocityModifier = num2;
				Utilities.SetStateChanged((CBaseEntity)(object)attacker.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
			attacker.PrintToCenterAlert($"红温降低！-{num}层 (剩余{_stacks[attacker]}/14)");
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_stacks.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		float interval = _config.Dices.Overheat.Interval;
		foreach (KeyValuePair<CCSPlayerController, float> item2 in _nextTickTime.ToList())
		{
			CCSPlayerController key = item2.Key;
			if (!(num >= item2.Value))
			{
				continue;
			}
			_nextTickTime[key] = num + interval;
			if (!_stacks.TryGetValue(key, out var value))
			{
				value = 0;
			}
			var (num2, num3, num4) = GetComboParams(key);
			if (value < num4)
			{
				value++;
				_stacks[key] = value;
				if ((CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)key.PlayerPawn.Value).IsValid)
				{
					SpeedBonusManager.Register(key, "Overheat", (float)value * num2);
					float num5 = 1f + SpeedBonusManager.GetEffective(key, 100f);
					key.PlayerPawn.Value.VelocityModifier = num5;
					Utilities.SetStateChanged((CBaseEntity)(object)key.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					float num6 = (float)value * num3;
					DamageBonusManager.Register(key, "Overheat", num6);
					if (value % 2 == 0 || value >= num4 - 2)
					{
						key.PrintToCenterAlert($"红温: {value}/{num4}层 速度+{(float)value * num2 * 100f:F0}% 伤害+{num6 * 100f:F0}%");
					}
				}
			}
			else if ((CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)key.PlayerPawn.Value).IsValid)
			{
				SpeedBonusManager.Register(key, "Overheat", (float)num4 * num2);
				float num7 = 1f + SpeedBonusManager.GetEffective(key, 100f);
				if (key.PlayerPawn.Value.VelocityModifier != num7)
				{
					key.PlayerPawn.Value.VelocityModifier = num7;
					Utilities.SetStateChanged((CBaseEntity)(object)key.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
		foreach (CCSPlayerController player in _players)
		{
			if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && _stacks.TryGetValue(player, out var value2))
			{
				float item = GetComboParams(player).speedPerStack;
				SpeedBonusManager.Register(player, "Overheat", (float)value2 * item);
				float num8 = 1f + SpeedBonusManager.GetEffective(player, 100f);
				if (player.PlayerPawn.Value.VelocityModifier != num8)
				{
					player.PlayerPawn.Value.VelocityModifier = num8;
					Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		if (DamageBonusManager.IsHighest(val, "Overheat"))
		{
			float effective = DamageBonusManager.GetEffective(val);
			info.Damage = (int)(info.Damage * (1f + effective));
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
