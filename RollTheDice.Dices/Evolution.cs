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

public class Evolution : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _nextEvolveTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _damageStacks = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _speedStacks = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _hpStacks = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Evolution";

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

	public Evolution(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Awakener");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "超进化", "超进化联动生效！");
			}
			float num = (DiceSynergy.HasPartner(player, "Awakener") ? (_config.Dices.Evolution.EvolveInterval / 2f) : _config.Dices.Evolution.EvolveInterval);
			_nextEvolveTime[player] = Server.CurrentTime + num;
			_damageStacks[player] = 0;
			_speedStacks[player] = 0;
			_hpStacks[player] = 0;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83e\uddec 进化开始！每25秒随机提升属性！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		RevertPlayer(player);
		_players.Remove(player);
		_nextEvolveTime.Remove(player);
		_damageStacks.Remove(player);
		_speedStacks.Remove(player);
		_hpStacks.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			RevertPlayer(item);
		}
		_players.Clear();
		_nextEvolveTime.Clear();
		_damageStacks.Clear();
		_speedStacks.Clear();
		_hpStacks.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void RevertPlayer(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
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
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid)
				{
					continue;
				}
				if (((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
				{
					RevertPlayer(item);
					_players.Remove(item);
					_nextEvolveTime.Remove(item);
					_damageStacks.Remove(item);
					_speedStacks.Remove(item);
					_hpStacks.Remove(item);
				}
				else
				{
					if (!_nextEvolveTime.TryGetValue(item, out var value) || num < value)
					{
						continue;
					}
					int maxStacks = _config.Dices.Evolution.MaxStacks;
					int num2 = _damageStacks.GetValueOrDefault(item) + _speedStacks.GetValueOrDefault(item) + _hpStacks.GetValueOrDefault(item);
					if (num2 >= maxStacks * 3)
					{
						float num3 = (DiceSynergy.HasPartner(item, "Awakener") ? (_config.Dices.Evolution.EvolveInterval / 2f) : _config.Dices.Evolution.EvolveInterval);
						_nextEvolveTime[item] = num + num3;
						continue;
					}
					int num4 = _random.Next(3);
					CCSPlayerPawn value2 = item.PlayerPawn.Value;
					string value4;
					switch (num4)
					{
					case 0:
					{
						int value5 = _damageStacks.GetValueOrDefault(item) + 1;
						_damageStacks[item] = value5;
						value4 = $"伤害+{_config.Dices.Evolution.DamagePerStack * 100f:F0}%";
						break;
					}
					case 1:
					{
						int num5 = _speedStacks.GetValueOrDefault(item) + 1;
						_speedStacks[item] = num5;
						float num6 = 1f + _config.Dices.Evolution.SpeedPerStack * (float)num5;
						value2.VelocityModifier = num6;
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
						value4 = $"移速+{_config.Dices.Evolution.SpeedPerStack * 100f:F0}%";
						break;
					}
					default:
					{
						int value3 = _hpStacks.GetValueOrDefault(item) + 1;
						_hpStacks[item] = value3;
						int hpPerStack = _config.Dices.Evolution.HpPerStack;
						((CBaseEntity)value2).MaxHealth += hpPerStack;
						((CBaseEntity)value2).Health += hpPerStack;
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
						value4 = $"HP上限+{hpPerStack}";
						break;
					}
					}
					float num7 = (DiceSynergy.HasPartner(item, "Awakener") ? (_config.Dices.Evolution.EvolveInterval / 2f) : _config.Dices.Evolution.EvolveInterval);
					_nextEvolveTime[item] = num + num7;
					item.PrintToCenterAlert($"\ud83e\uddec 进化！{value4} | 总层数：{_damageStacks.GetValueOrDefault(item)}+{_speedStacks.GetValueOrDefault(item)}+{_hpStacks.GetValueOrDefault(item)}");
					continue;
				}
			}
			catch
			{
			}
		}
		foreach (KeyValuePair<CCSPlayerController, int> speedStack in _speedStacks)
		{
			CCSPlayerController key = speedStack.Key;
			if (speedStack.Value > 0 && (CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)key.PlayerPawn.Value).IsValid)
			{
				float num8 = 1f + _config.Dices.Evolution.SpeedPerStack * (float)speedStack.Value;
				if (Math.Abs(key.PlayerPawn.Value.VelocityModifier - num8) > 0.01f)
				{
					key.PlayerPawn.Value.VelocityModifier = num8;
					Utilities.SetStateChanged((CBaseEntity)(object)key.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		if (_damageStacks.Count == 0)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_damageStacks.TryGetValue(val, out var value3) || value3 <= 0)
		{
			return (HookResult)0;
		}
		float num = 1f + _config.Dices.Evolution.DamagePerStack * (float)value3;
		info.Damage *= num;
		return (HookResult)1;
	}
}
