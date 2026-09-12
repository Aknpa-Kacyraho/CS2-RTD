using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Eclipse : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextPhaseTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, bool> _isNewMoon = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "Eclipse";

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

	public Eclipse(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_isNewMoon[player] = true;
			_nextPhaseTime[player] = Server.CurrentTime + _config.Dices.Eclipse.NewMoonDuration;
			ApplyPhase(player, newMoon: true);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83c\udf11 新月降临！速度×1.2，半透明，伤害×0.8...");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		Revert(player);
		_players.Remove(player);
		_nextPhaseTime.Remove(player);
		_isNewMoon.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Revert(item);
		}
		_players.Clear();
		_nextPhaseTime.Clear();
		_isNewMoon.Clear();
	}

	private void Revert(CCSPlayerController player)
	{
		DamageBonusManager.Unregister(player, "Eclipse");
		SpeedBonusManager.Unregister(player, "Eclipse");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	private void ApplyPhase(CCSPlayerController player, bool newMoon)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (newMoon)
			{
				SpeedBonusManager.Register(player, "Eclipse", _config.Dices.Eclipse.NewMoonSpeed - 1f);
				value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				((CBaseModelEntity)value).Render = Color.FromArgb(100, 180, 180, 220);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			}
			else
			{
				SpeedBonusManager.Register(player, "Eclipse", _config.Dices.Eclipse.FullMoonSpeed - 1f);
				value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				((CBaseModelEntity)value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			}
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			DamageBonusManager.Register(player, "Eclipse", (newMoon ? _config.Dices.Eclipse.NewMoonDamageMult : _config.Dices.Eclipse.FullMoonDamageMult) - 1f);
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
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && _nextPhaseTime.TryGetValue(item, out var value) && !(num < value) && _isNewMoon.TryGetValue(item, out var value2))
				{
					bool flag = !value2;
					_isNewMoon[item] = flag;
					if (flag)
					{
						_nextPhaseTime[item] = num + _config.Dices.Eclipse.NewMoonDuration;
						ApplyPhase(item, newMoon: true);
						item.PrintToCenterAlert("\ud83c\udf11 新月！速度×1.2 半透明 伤害×0.8");
					}
					else
					{
						_nextPhaseTime[item] = num + _config.Dices.Eclipse.FullMoonDuration;
						ApplyPhase(item, newMoon: false);
						item.PrintToCenterAlert("\ud83c\udf15 满月！速度×0.8 伤害×1.5");
					}
				}
			}
			catch
			{
			}
		}
	}
}
