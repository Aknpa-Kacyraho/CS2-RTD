using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class SoulEater : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private static readonly string[] ThrowablePool = new string[6] { "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_incgrenade", "weapon_decoy" };

	public override string ClassName => "SoulEater";

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

	public SoulEater(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
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
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		//IL_016b: Expected O, but got Unknown
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)attacker.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = userid.PlayerPawn;
		object obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value = playerPawn.Value;
			obj = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
		}
		if (obj == null)
		{
			return (HookResult)0;
		}
		Vector absOrigin = ((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin;
		// 走统一特效系统（预缓存 + 生命周期管理；仍挂到击杀者身上跟随）。
		Effects.PlayOnPlayer(attacker, ParticlePaths.ExperienceRing, 2f);
		int num = _random.Next(_config.Dices.SoulEater.HealMin, _config.Dices.SoulEater.HealMax + 1);
		CCSPlayerPawn value2 = attacker.PlayerPawn.Value;
		int num2 = Math.Min(((CBaseEntity)value2).Health + num, ((CBaseEntity)value2).MaxHealth);
		int value3 = num2 - ((CBaseEntity)value2).Health;
		((CBaseEntity)value2).Health = num2;
		Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
		CCSPlayerController capturedAttacker = attacker;
		Server.NextFrame((Action)delegate
		{
			if (!((CEntityInstance)(object)capturedAttacker == (CEntityInstance)null) && ((CEntityInstance)capturedAttacker).IsValid)
			{
				for (int i = 0; i < 2; i++)
				{
					string text = ThrowablePool[_random.Next(ThrowablePool.Length)];
					capturedAttacker.GiveNamedItem(text);
				}
			}
		});
		if (DiceSynergy.HasPartner(attacker, "Vampire"))
		{
			value2.ArmorValue = Math.Min(value2.ArmorValue + 25, 100);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_ArmorValue", 0);
		}
		attacker.PrintToCenterAlert($"\ud83d\udc7b 噬魂 +{value3} HP! +2 投掷物!" + (DiceSynergy.HasPartner(attacker, "Vampire") ? " +25甲" : ""));
		return (HookResult)0;
	}
}
