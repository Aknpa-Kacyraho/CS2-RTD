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

public class God : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalArmor = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "God";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			return list;
		}
	}

	public God(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
			_originalArmor[player] = value.ArmorValue;
			((CBaseEntity)value).MaxHealth = 666;
			((CBaseEntity)value).Health = 666;
			value.ArmorValue = 666;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			SpeedBonusManager.Register(player, "God", 1f);
			value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Goddess");
			DamageBonusManager.Register(player, "God", _comboActive ? 2f : 0.5f);
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "神之共鸣", "上帝伤害翻倍+女神多祝福一人！");
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
		DamageBonusManager.Unregister(player, "God");
		SpeedBonusManager.Unregister(player, "God");
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
			if (_originalArmor.TryGetValue(player, out var value3))
			{
				value.ArmorValue = Math.Min(value.ArmorValue, value3);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				_originalArmor.Remove(player);
			}
			value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
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
		_originalArmor.Clear();
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
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
				{
					CCSPlayerPawn value = item.PlayerPawn.Value;
					float expected = 1f + SpeedBonusManager.GetEffective(item, 100f);
					if (value.VelocityModifier < expected - 0.5f)
					{
						value.VelocityModifier = expected;
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
				}
			}
			catch
			{
			}
		}
	}
}
