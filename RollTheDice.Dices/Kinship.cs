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
/// 羁绊 Kinship：队友（同队存活真人）阵亡时自己获得短暂无敌；自己阵亡时所有存活队友获得短暂无敌。
/// 无敌用 OnPlayerTakeDamagePre 将伤害归零实现（本版本 TakesDamage=false 单独不可靠，Void/Fool/Prophet 均如此）。
/// </summary>
public class Kinship : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _invulnUntil = new Dictionary<ulong, float>();

	private readonly HashSet<ulong> _holderIds = new HashSet<ulong>();

	public override string ClassName => "Kinship";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public Kinship(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_holderIds.Add(((CBasePlayerController)player).SteamID);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			if (DiceSynergy.HasPartner(player, "DivineResurrection"))
			{
				DiceSynergy.AnnounceCombo(player, "生死与共", "羁绊无敌翻倍，复活队友附带 2s 无敌！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid)
		{
			ulong steamID = ((CBasePlayerController)player).SteamID;
			_holderIds.Remove(steamID);
			_invulnUntil.Remove(steamID);
		}
	}

	public override void Reset()
	{
		_players.Clear();
		_holderIds.Clear();
		_invulnUntil.Clear();
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
		if (_holderIds.Contains(((CBasePlayerController)victim).SteamID))
		{
			foreach (CCSPlayerController teammate in GetAliveTeammates(victim))
			{
				GrantInvulnerability(teammate);
			}
		}
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if ((CEntityInstance)(object)holder == (CEntityInstance)null || !((CEntityInstance)holder).IsValid || (CEntityInstance)(object)holder == (CEntityInstance)(object)victim || ((CBaseEntity)holder).TeamNum != ((CBaseEntity)victim).TeamNum || (CEntityInstance)(object)holder.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)holder.PlayerPawn.Value).IsValid || ((CBaseEntity)holder.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			GrantInvulnerability(holder);
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

	private void GrantInvulnerability(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if ((CEntityInstance)(object)pawn == (CEntityInstance)null || !((CEntityInstance)pawn).IsValid || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		float seconds = _config.Dices.Kinship.InvulnSeconds;
		if (DiceSynergy.HasPartner(player, "DivineResurrection"))
		{
			seconds *= 2f;
		}
		_invulnUntil[((CBasePlayerController)player).SteamID] = Server.CurrentTime + seconds;
		player.PrintToCenterAlert($"\ud83e\udd1d 羁绊庇护！{seconds:F0}s 无敌！");
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_invulnUntil.Count == 0)
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
		if (!_invulnUntil.TryGetValue(steamID, out float until))
		{
			return (HookResult)0;
		}
		if (Server.CurrentTime >= until)
		{
			_invulnUntil.Remove(steamID);
			return (HookResult)0;
		}
		info.Damage = 0f;
		return (HookResult)1;
	}
}
