using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

/// <summary>
/// 黄昏之时 Twilight：开局把敌我两两配对互换位置（一次性，区别于 ChaosStorm 的周期全员随机）。
/// </summary>
public class Twilight : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private bool _swapped;

	public override string ClassName => "Twilight";

	public Twilight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if (!_swapped)
		{
			_swapped = true;
			Server.NextFrame(SwapPaired);
		}
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
		_swapped = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	private void SwapPaired()
	{
		List<CCSPlayerController> alive = Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			CCSPlayerPawn pawn = p?.PlayerPawn?.Value;
			return p != null && p.IsValid && !((CBasePlayerController)p).IsHLTV && pawn != null && pawn.IsValid && ((CBaseEntity)pawn).LifeState == 0 && ((CBaseEntity)pawn).AbsOrigin != null;
		}).ToList();
		if (alive.Count < 2)
		{
			return;
		}
		List<CCSPlayerController> t = alive.Where((CCSPlayerController p) => ((CBaseEntity)p).TeamNum == 2).OrderBy((CCSPlayerController _) => _random.Next()).ToList();
		List<CCSPlayerController> ct = alive.Where((CCSPlayerController p) => ((CBaseEntity)p).TeamNum == 3).OrderBy((CCSPlayerController _) => _random.Next()).ToList();
		int pairs = Math.Min(t.Count, ct.Count);
		for (int i = 0; i < pairs; i++)
		{
			CCSPlayerController a = t[i];
			CCSPlayerController b = ct[i];
			Vector posA = ((CBaseEntity)a.PlayerPawn.Value).AbsOrigin;
			Vector posB = ((CBaseEntity)b.PlayerPawn.Value).AbsOrigin;
			if (posA == null || posB == null)
			{
				continue;
			}
			((CBaseEntity)a.PlayerPawn.Value).Teleport(posB, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
			((CBaseEntity)b.PlayerPawn.Value).Teleport(posA, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "🌅 黄昏之时！敌我位置互换！");
	}
}
