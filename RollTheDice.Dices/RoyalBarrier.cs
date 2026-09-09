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

public class RoyalBarrier : DiceBlueprint
{
	private bool _comboActive;

	public override string ClassName => "RoyalBarrier";

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

	public RoyalBarrier(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseEntity)value).MaxHealth = _config.Dices.RoyalBarrier.MaxHealth;
			((CBaseEntity)value).Health = _config.Dices.RoyalBarrier.MaxHealth;
			value.ArmorValue = _config.Dices.RoyalBarrier.MaxArmor;
			_comboActive = DiceSynergy.HasPartner(player, "Giant");
			float num = (_comboActive ? 0.65f : _config.Dices.RoyalBarrier.SpeedMultiplier);
			value.VelocityModifier = num;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "钢铁要塞", "巨人撑起堡垒！移速提升至 65%，跳跃恢复");
			}
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"health",
					_config.Dices.RoyalBarrier.MaxHealth.ToString()
				},
				{
					"armor",
					_config.Dices.RoyalBarrier.MaxArmor.ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
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
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		if (_players.Count == 0)
		{
			return;
		}
		int maxHealth = _config.Dices.RoyalBarrier.MaxHealth;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
				{
					CCSPlayerPawn value = item.PlayerPawn.Value;
					bool comboActive = DiceSynergy.HasPartner(item, "Giant");
					float num = (comboActive ? 0.65f : _config.Dices.RoyalBarrier.SpeedMultiplier);
					bool flag = !comboActive;
					if (((CBaseEntity)value).MaxHealth != maxHealth)
					{
						((CBaseEntity)value).MaxHealth = maxHealth;
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
					}
					if (Math.Abs(value.VelocityModifier - num) > 0.01f)
					{
						value.VelocityModifier = num;
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
					if (flag && ((CBaseEntity)value).AbsVelocity.Z > 50f)
					{
						((CBaseEntity)value).Teleport(((CBaseEntity)value).AbsOrigin, ((CBaseEntity)value).AbsRotation, new Vector((float?)((CBaseEntity)value).AbsVelocity.X, (float?)((CBaseEntity)value).AbsVelocity.Y, (float?)0f));
					}
				}
			}
			catch
			{
			}
		}
	}
}
