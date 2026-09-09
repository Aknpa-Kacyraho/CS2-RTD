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

public class Bugle : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private float _startTime;

	private bool _expired;

	public override string ClassName => "Bugle";

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

	public Bugle(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		if (_startTime == 0f)
		{
			_startTime = Server.CurrentTime;
		}
		_expired = false;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			SpeedBonusManager.Register(item, "Bugle", _config.Dices.Bugle.SpeedMultiplier - 1f);
			DamageBonusManager.Register(item, "Bugle", _config.Dices.Bugle.DamageMultiplier - 1f);
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Bugle_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
		_comboActive = DiceSynergy.HasPartner(player, "World");
		if (!_comboActive)
		{
			return;
		}
		DiceSynergy.AnnounceCombo(player, "天启", "冲锋号+世界！天启降临！");
		List<CCSPlayerController> list = (from _ in Utilities.GetPlayers()
			where ((CEntityInstance)_).IsValid && !((CBasePlayerController)_).IsHLTV && (CEntityInstance)(object)_ != (CEntityInstance)(object)player && (CEntityInstance)(object)_.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)_.PlayerPawn.Value).IsValid && ((CBaseEntity)_.PlayerPawn.Value).LifeState == 0
			orderby _random.Next()
			select _).Take(2).ToList();
		RollTheDice instance = RollTheDice.Instance;
		foreach (CCSPlayerController item2 in list)
		{
			instance?.ForceDiceForPlayer(item2);
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if (_players.Count == 0)
		{
			ClearAllBuffs();
		}
	}

	public override void Reset()
	{
		try
		{
			ClearAllBuffs();
		}
		catch
		{
		}
		_players.Clear();
		_startTime = 0f;
		_expired = true;
	}

	public override void Destroy()
	{
		try
		{
			Reset();
		}
		catch
		{
			_players.Clear();
		}
	}

	private void ClearAllBuffs()
	{
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid
			select p)
		{
			SpeedBonusManager.Unregister(item, "Bugle");
			DamageBonusManager.Unregister(item, "Bugle");
		}
	}

	public void OnTick()
	{
		if (_players.Count == 0 || _expired)
		{
			return;
		}
		float num = Server.CurrentTime;
		float num2 = num - _startTime;
		float num3 = _config.Dices.Bugle.Duration - num2;
		if (num3 <= 10f && num3 > 9.7f)
		{
			foreach (CCSPlayerController item in _players.ToList())
			{
				if (item != null)
				{
					item.PrintToCenterAlert("\ud83d\udcef 冲锋号还剩10秒！");
				}
			}
		}
		if (num3 <= 5f && num3 > 4.7f)
		{
			foreach (CCSPlayerController item2 in _players.ToList())
			{
				if (item2 != null)
				{
					item2.PrintToCenterAlert("\ud83d\udcef 冲锋号还剩5秒！");
				}
			}
		}
		if (num2 >= _config.Dices.Bugle.Duration)
		{
			_expired = true;
			ClearAllBuffs();
			foreach (CCSPlayerController item3 in _players.ToList())
			{
				if (!((CEntityInstance)(object)item3 == (CEntityInstance)null) && ((CEntityInstance)item3).IsValid && !((CEntityInstance)(object)item3.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item3.PlayerPawn.Value).IsValid && ((CBaseEntity)item3.PlayerPawn.Value).LifeState == 0)
				{
					float effective = SpeedBonusManager.GetEffective(item3);
					item3.PlayerPawn.Value.VelocityModifier = 1f + effective;
					Utilities.SetStateChanged((CBaseEntity)(object)item3.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udcef 冲锋号结束！全员效果消退！");
			return;
		}
		foreach (CCSPlayerController item4 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			if (SpeedBonusManager.HasAny(item4))
			{
				float effective2 = SpeedBonusManager.GetEffective(item4);
				item4.PlayerPawn.Value.VelocityModifier = 1f + effective2;
				Utilities.SetStateChanged((CBaseEntity)(object)item4.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid || _expired)
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
		CCSPlayerController attacker2 = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)attacker2 == (CEntityInstance)null || !((CEntityInstance)attacker2).IsValid)
		{
			return (HookResult)0;
		}
		if (SpeedBonusManager.HasAny(attacker2) && _players.Any((CCSPlayerController p) => ((CBaseEntity)p).TeamNum == ((CBaseEntity)attacker2).TeamNum) && DamageBonusManager.IsHighest(attacker2, "Bugle"))
		{
			float effective = DamageBonusManager.GetEffective(attacker2);
			if (effective > 0f)
			{
				info.Damage = (int)(info.Damage * (1f + effective));
				return (HookResult)1;
			}
		}
		return (HookResult)0;
	}
}
