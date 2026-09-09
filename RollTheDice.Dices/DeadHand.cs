using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DeadHand : DiceBlueprint
{
	private bool _comboActive;

	public override string ClassName => "DeadHand";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventWeaponFire";
			return list;
		}
	}

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

	public DeadHand(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "DeagleKing");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "致命一击", "开枪自伤减半+命中回血翻倍！");
			}
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_DeadHand_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
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
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)userid.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)userid.PlayerPawn.Value).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		CCSPlayerPawn value = userid.PlayerPawn.Value;
		int num = (DiceSynergy.HasPartner(userid, "DeagleKing") ? (_config.Dices.DeadHand.SelfDamage / 2) : _config.Dices.DeadHand.SelfDamage);
		((CBaseEntity)value).Health = Math.Max(0, ((CBaseEntity)value).Health - num);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
		if (((CBaseEntity)value).Health <= 0)
		{
			if (!userid.IsBot && !((CBasePlayerController)userid).IsHLTV)
			{
				((CBasePlayerPawn)value).CommitSuicide(false, true);
			}
			else
			{
				try
				{
					((CBasePlayerPawn)value).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)value).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				}
			}
		}
		return (HookResult)0;
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
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
		CCSPlayerPawn val2 = val.PlayerPawn?.Value;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid)
		{
			return (HookResult)0;
		}
		int num = (DiceSynergy.HasPartner(val, "DeagleKing") ? 60 : 30);
		((CBaseEntity)val2).Health = Math.Min(((CBaseEntity)val2).Health + num, ((CBaseEntity)val2).MaxHealth);
		Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
		return (HookResult)0;
	}
}
