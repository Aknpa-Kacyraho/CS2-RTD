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

public class Prophet : DiceBlueprint
{
	private float _lastRevealTime;

	public override string ClassName => "Prophet";

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

	public Prophet(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_lastRevealTime = 0f;
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
		float num = Server.CurrentTime;
		if (num - _lastRevealTime < 5f)
		{
			return;
		}
		_lastRevealTime = num;
		foreach (CCSPlayerController prophet in _players.ToList())
		{
			if ((CEntityInstance)(object)prophet == (CEntityInstance)null || !((CEntityInstance)prophet).IsValid)
			{
				continue;
			}
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)prophet).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p).ToList();
			RollTheDice instance = RollTheDice.Instance;
			if (instance == null)
			{
				continue;
			}
			List<string> list2 = new List<string>();
			foreach (CCSPlayerController item in list)
			{
				List<string> allDiceForPlayer = instance.GetAllDiceForPlayer(item);
				if (allDiceForPlayer.Count > 0)
				{
					string text = string.Join("+", allDiceForPlayer.Select(delegate(string d)
					{
						string text2 = _localizer.GetString("dice_" + d + "_name", d);
						return (d == "Trickster") ? (text2 + "(真实)") : text2;
					}));
					list2.Add(((CBasePlayerController)item).PlayerName + "[" + text + "]");
				}
			}
			if (list2.Count > 0)
			{
				prophet.PrintToCenterAlert("\ud83d\udd2e 敌人骰子：" + string.Join(" | ", list2));
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
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
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid)
		{
			return (HookResult)0;
		}
		RollTheDice instance = RollTheDice.Instance;
		if (instance == null)
		{
			return (HookResult)0;
		}
		List<string> allDiceForPlayer = instance.GetAllDiceForPlayer(val2);
		if (allDiceForPlayer.Count > 0)
		{
			info.Damage = (int)(info.Damage * 1.3f);
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
