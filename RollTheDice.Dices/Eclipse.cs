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
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
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
				value.VelocityModifier = _config.Dices.Eclipse.NewMoonSpeed;
				((CBaseModelEntity)value).Render = Color.FromArgb(100, 180, 180, 220);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			}
			else
			{
				value.VelocityModifier = _config.Dices.Eclipse.FullMoonSpeed;
				((CBaseModelEntity)value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			}
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
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

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
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
		if (!_isNewMoon.TryGetValue(val, out var value3))
		{
			return (HookResult)0;
		}
		if (value3)
		{
			info.Damage *= _config.Dices.Eclipse.NewMoonDamageMult;
		}
		else
		{
			info.Damage *= _config.Dices.Eclipse.FullMoonDamageMult;
		}
		return (HookResult)1;
	}
}
