using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Taotie : DiceBlueprint
{
	private readonly HashSet<ulong> _alreadyStolenThisTick = new HashSet<ulong>();

	public override string ClassName => "Taotie";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public Taotie(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
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
		_alreadyStolenThisTick.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
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
		CCSPlayerPawn obj3 = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj4;
		if (obj3 == null)
		{
			obj4 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj3).Controller;
			if (controller2 == null)
			{
				obj4 = null;
			}
			else
			{
				CBasePlayerController value3 = controller2.Value;
				obj4 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj4;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val == (CEntityInstance)(object)val2 || ((CBasePlayerController)val2).IsHLTV)
		{
			return (HookResult)0;
		}
		if (_alreadyStolenThisTick.Contains(((CBasePlayerController)val2).SteamID))
		{
			return (HookResult)0;
		}
		_alreadyStolenThisTick.Add(((CBasePlayerController)val2).SteamID);
		CCSPlayerPawn val3 = val.PlayerPawn?.Value;
		if ((CEntityInstance)(object)val3 == (CEntityInstance)null || !((CEntityInstance)val3).IsValid)
		{
			return (HookResult)0;
		}
		RollTheDice instance = RollTheDice.Instance;
		if (instance != null)
		{
			List<string> allDiceForPlayer = instance.GetAllDiceForPlayer(val2);
			if (allDiceForPlayer != null && allDiceForPlayer.Count > 0)
			{
				foreach (string item in allDiceForPlayer)
				{
					instance.RemoveDiceFromPlayer(val2, item);
				}
				((CBaseEntity)val3).Health += _config.Dices.Taotie.HpPerEat;
				Utilities.SetStateChanged((CBaseEntity)(object)val3, "CBaseEntity", "m_iHealth", 0);
				val.PrintToCenterAlert($"\ud83c\udf56 饕餮吞噬！+{_config.Dices.Taotie.HpPerEat}HP！");
				val2.PrintToCenterAlert("\ud83c\udf56 被饕餮吞噬了骰子！");
			}
		}
		return (HookResult)0;
	}
}
