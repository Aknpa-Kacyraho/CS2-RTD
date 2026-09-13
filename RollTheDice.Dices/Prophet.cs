using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Prophet : DiceBlueprint
{
	private readonly Dictionary<ulong, int> _stacks = new Dictionary<ulong, int>();

	private readonly Dictionary<ulong, float> _nextGrant = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, int> _lastBlockTick = new Dictionary<ulong, int>();

	public override string ClassName => "Prophet";

	public override List<string> Listeners => new List<string> { "OnTick", "OnPlayerTakeDamagePre" };

	public Prophet(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		ulong steamID = ((CBasePlayerController)player).SteamID;
		_stacks[steamID] = 0;
		_nextGrant[steamID] = Server.CurrentTime + _config.Dices.Prophet.GainInterval;
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
		if ((CEntityInstance)(object)player == (CEntityInstance)null)
		{
			return;
		}
		_players.Remove(player);
		ulong steamID = ((CBasePlayerController)player).SteamID;
		_stacks.Remove(steamID);
		_nextGrant.Remove(steamID);
		_lastBlockTick.Remove(steamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_stacks.Clear();
		_nextGrant.Clear();
		_lastBlockTick.Clear();
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
		float gainInterval = _config.Dices.Prophet.GainInterval;
		int maxStacks = _config.Dices.Prophet.MaxStacks;
		if (gainInterval <= 0f || maxStacks <= 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (CCSPlayerController prophet in _players.ToList())
		{
			if ((CEntityInstance)(object)prophet == (CEntityInstance)null || !((CEntityInstance)prophet).IsValid)
			{
				continue;
			}
			ulong steamID = ((CBasePlayerController)prophet).SteamID;
			float next = (_nextGrant.TryGetValue(steamID, out float value) ? value : now);
			if (now < next)
			{
				continue;
			}
			int current = (_stacks.TryGetValue(steamID, out int value2) ? value2 : 0);
			_stacks[steamID] = Math.Min(current + 1, maxStacks);
			_nextGrant[steamID] = now + gainInterval;
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info.Damage <= 0f)
		{
			return (HookResult)0;
		}
		CCSPlayerController victim = ResolvePlayerController(entity);
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		ulong steamID = ((CBasePlayerController)victim).SteamID;
		if (!_stacks.TryGetValue(steamID, out int stacks) || stacks <= 0)
		{
			return (HookResult)0;
		}
		CCSPlayerController attacker = ResolvePlayerController(info.Attacker?.Value);
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || ((CBasePlayerController)attacker).IsHLTV || ((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return (HookResult)0;
		}
		if (_lastBlockTick.TryGetValue(steamID, out int lastTick) && lastTick == Server.TickCount)
		{
			return (HookResult)0;
		}
		_lastBlockTick[steamID] = Server.TickCount;
		int remaining = stacks - 1;
		_stacks[steamID] = remaining;
		info.Damage = 0f;
		victim.PrintToCenterAlert($"\ud83d\udee1 预知抵挡！剩余 {remaining} 层");
		return (HookResult)1;
	}

	private static CCSPlayerController ResolvePlayerController(CBaseEntity entity)
	{
		if ((CEntityInstance)(object)entity == (CEntityInstance)null)
		{
			return null;
		}
		CCSPlayerPawn pawn = ((NativeObject)entity).As<CCSPlayerPawn>();
		if ((CEntityInstance)(object)pawn == (CEntityInstance)null)
		{
			return null;
		}
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null || controller.Value == null)
		{
			return null;
		}
		return ((NativeObject)controller.Value).As<CCSPlayerController>();
	}
}
