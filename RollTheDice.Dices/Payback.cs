using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Payback : DiceBlueprint
{
	public override string ClassName => "Payback";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

	public Payback(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid) || (CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid || (CEntityInstance)(object)attacker.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)attacker.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		CCSPlayerController capturedAttacker = attacker;
		int damage = Random.Shared.Next(_config.Dices.Payback.DamageMin, _config.Dices.Payback.DamageMax + 1);
		Server.NextFrame((Action)delegate
		{
			if (!((CEntityInstance)(object)capturedAttacker == (CEntityInstance)null) && ((CEntityInstance)capturedAttacker).IsValid && !((CEntityInstance)(object)capturedAttacker.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)capturedAttacker.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value = capturedAttacker.PlayerPawn.Value;
				((CBaseEntity)value).Health -= damage;
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				if (capturedAttacker.InGameMoneyServices != null)
				{
					capturedAttacker.InGameMoneyServices.Account = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)capturedAttacker, "CCSPlayerController", "m_pInGameMoneyServices", 0);
				}
				if (((CBaseEntity)value).Health <= 0 && ((CBaseEntity)value).LifeState == 0)
				{
					if (!capturedAttacker.IsBot && !((CBasePlayerController)capturedAttacker).IsHLTV)
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
					else
					{
						((CBaseEntity)value).Health = 0;
						Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
					}
				}
				capturedAttacker.PrintToCenterAlert($"☠ 以牙还牙! -{damage} HP + 金钱清零!");
			}
		});
		return (HookResult)0;
	}
}
