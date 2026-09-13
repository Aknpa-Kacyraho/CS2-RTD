using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 怨念 Curse：持有者阵亡时标记击杀者（短时间受到伤害提高），并给存活队友一段伤害加成。
/// </summary>
public class Curse : DiceBlueprint
{
	private readonly HashSet<ulong> _holderIds = new HashSet<ulong>();

	private readonly Dictionary<ulong, float> _markUntil = new Dictionary<ulong, float>();

	public override string ClassName => "Curse";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public Curse(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Add(CCSPlayerController player)
	{
		base.Add(player);
		if (_players.Contains(player))
		{
			_holderIds.Add(player.SteamID);
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		_players.Remove(player);
		_holderIds.Remove(player.SteamID);
		_markUntil.Remove(player.SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_holderIds.Clear();
		_markUntil.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		if (!_holderIds.Contains(((CBasePlayerController)victim).SteamID))
		{
			return (HookResult)0;
		}
		CCSPlayerController killer = @event.Attacker;
		if ((CEntityInstance)(object)killer != (CEntityInstance)null && ((CEntityInstance)killer).IsValid && (CEntityInstance)(object)killer != (CEntityInstance)(object)victim && ((CBaseEntity)killer).TeamNum != ((CBaseEntity)victim).TeamNum)
		{
			_markUntil[((CBasePlayerController)killer).SteamID] = Server.CurrentTime + _config.Dices.Curse.MarkSeconds;
			killer.PrintToCenterAlert("怨念缠身！");
		}
		foreach (CCSPlayerController teammate in GetAliveTeammates(victim))
		{
			DamageBonusManager.Register(teammate, "Curse", _config.Dices.Curse.TeamDamageBonus, null, _config.Dices.Curse.TeamBonusSeconds);
		}
		return (HookResult)0;
	}

	private static IEnumerable<CCSPlayerController> GetAliveTeammates(CCSPlayerController of)
	{
		return Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			if (!((CEntityInstance)p).IsValid || ((CBasePlayerController)p).IsHLTV || p.IsBot)
			{
				return false;
			}
			if ((CEntityInstance)(object)p == (CEntityInstance)(object)of || ((CBaseEntity)p).TeamNum != ((CBaseEntity)of).TeamNum)
			{
				return false;
			}
			return (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0;
		});
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_markUntil.Count == 0)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn pawn = ((NativeObject)entity).As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return (HookResult)0;
		}
		object obj;
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null)
		{
			obj = null;
		}
		else
		{
			CBasePlayerController value = controller.Value;
			obj = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
		}
		CCSPlayerController player = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return (HookResult)0;
		}
		ulong steamID = ((CBasePlayerController)player).SteamID;
		if (!_markUntil.TryGetValue(steamID, out float until))
		{
			return (HookResult)0;
		}
		if (Server.CurrentTime >= until)
		{
			_markUntil.Remove(steamID);
			return (HookResult)0;
		}
		info.Damage *= 1f + _config.Dices.Curse.MarkDamageBonus;
		return (HookResult)1;
	}
}
