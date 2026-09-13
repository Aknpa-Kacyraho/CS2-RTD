using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

/// <summary>
/// 以牙还牙 Payback：死亡时掠夺击杀者——扣血、金钱清零、弹匣清空。
/// </summary>
public class Payback : DiceBlueprint
{
	public override string ClassName => "Payback";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public Payback(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
		{
			return;
		}
		_players.Add(player);
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
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

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (victim == null || !victim.IsValid || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		if (attacker == null || !attacker.IsValid || attacker == victim)
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		CCSPlayerController captured = attacker;
		PaybackConfig cfg = _config.Dices.Payback;
		Server.NextFrame(delegate
		{
			CCSPlayerPawn pawn = captured?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid)
			{
				return;
			}
			CBaseEntity entity = pawn;
			entity.Health -= cfg.Hp;
			Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
			if (cfg.ClearMoney && captured.InGameMoneyServices != null)
			{
				captured.InGameMoneyServices.Account = 0;
				Utilities.SetStateChanged(captured, "CCSPlayerController", "m_pInGameMoneyServices", 0);
			}
			CBasePlayerWeapon weapon = pawn.WeaponServices?.ActiveWeapon?.Value;
			if (weapon != null && weapon.IsValid && weapon.Clip1 > 0)
			{
				weapon.Clip1 = 0;
				Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1", 0);
			}
			if (entity.Health <= 0 && entity.LifeState == 0)
			{
				if (!captured.IsBot && !((CBasePlayerController)captured).IsHLTV)
				{
					try
					{
						((CBasePlayerPawn)pawn).CommitSuicide(false, true);
					}
					catch
					{
						entity.Health = 0;
						Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
					}
				}
				else
				{
					entity.Health = 0;
					Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
				}
			}
			captured.PrintToCenterAlert($"💀 以牙还牙！-{cfg.Hp} HP，金钱与弹匣清零！");
		});
		return HookResult.Continue;
	}
}
