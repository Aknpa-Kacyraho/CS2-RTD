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

public class Paladin : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _paladinSpeedBonus = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _paladinHpBonus = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Paladin";

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

	public Paladin(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_paladinSpeedBonus[player] = 0f;
			_paladinHpBonus[player] = 0;
			CCSPlayerPawn value = player.PlayerPawn.Value;
			int bonusArmor = _config.Dices.Paladin.BonusArmor;
			value.ArmorValue = Math.Max(value.ArmorValue, bonusArmor);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83d\udee1 圣骑士！每次护甲受损强化自己...");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		Revert(player);
		_players.Remove(player);
		_paladinSpeedBonus.Remove(player);
		_paladinHpBonus.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Revert(item);
		}
		_players.Clear();
		_paladinSpeedBonus.Clear();
		_paladinHpBonus.Clear();
	}

	private void Revert(CCSPlayerController player)
	{
		SpeedBonusManager.Unregister(player, "Paladin");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		CCSPlayerPawn value2 = val.PlayerPawn.Value;
		int armorValue = value2.ArmorValue;
		if (armorValue > 0 && info.Damage > 0f)
		{
			float num = (_paladinSpeedBonus.TryGetValue(val, out var value3) ? value3 : 0f);
			int num2 = (_paladinHpBonus.TryGetValue(val, out var value4) ? value4 : 0);
			if (num < 0.5f)
			{
				num = Math.Min(num + 0.1f, 0.5f);
				_paladinSpeedBonus[val] = num;
			}
			num2 += 10;
			_paladinHpBonus[val] = num2;
			SpeedBonusManager.Register(val, "Paladin", num);
			value2.VelocityModifier = 1f + SpeedBonusManager.GetEffective(val, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			((CBaseEntity)value2).MaxHealth += 10;
			((CBaseEntity)value2).Health += 10;
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			val.PrintToCenterAlert($"\ud83d\udee1 圣骑士强化！速度 ×{1f + num:F1} | HP +{num2}");
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && _paladinSpeedBonus.TryGetValue(item, out var value) && !(value <= 0f))
				{
					CCSPlayerPawn value2 = item.PlayerPawn.Value;
					SpeedBonusManager.Register(item, "Paladin", value);
					float num = 1f + SpeedBonusManager.GetEffective(item, 100f);
					if (Math.Abs(value2.VelocityModifier - num) > 0.01f)
					{
						value2.VelocityModifier = num;
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
				}
			}
			catch
			{
			}
		}
	}
}
