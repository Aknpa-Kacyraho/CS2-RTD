using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

/// <summary>
/// 羁绊 Kinship：队友（同队存活真人）阵亡时自己获得短暂无敌；自己阵亡时所有存活队友获得短暂无敌。
/// </summary>
public class Kinship : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _invulnUntil = new Dictionary<ulong, float>();

	public override string ClassName => "Kinship";

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

	public Kinship(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid)
		{
			_invulnUntil.Remove(((CBasePlayerController)player).SteamID);
		}
	}

	public override void Reset()
	{
		_players.Clear();
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
		if (_players.Contains(victim))
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
		ulong sid = ((CBasePlayerController)player).SteamID;
		_invulnUntil[sid] = Server.CurrentTime + seconds;
		((CBaseEntity)pawn).TakesDamage = false;
		player.PrintToCenterAlert($"\ud83e\udd1d 羁绊庇护！{seconds:F0}s 无敌！");
		new Timer(seconds, (Action)delegate
		{
			if (_invulnUntil.TryGetValue(sid, out float until) && Server.CurrentTime >= until - 0.05f)
			{
				_invulnUntil.Remove(sid);
				CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == sid);
				if ((CEntityInstance)(object)((val == null) ? null : val.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)val.PlayerPawn.Value).IsValid)
				{
					((CBaseEntity)val.PlayerPawn.Value).TakesDamage = true;
				}
			}
		}, (TimerFlags?)null);
	}
}
