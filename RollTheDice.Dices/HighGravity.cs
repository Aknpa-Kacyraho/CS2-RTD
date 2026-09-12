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

public class HighGravity : DiceBlueprint
{
	public readonly Random _random = new Random();

	public override string ClassName => "HighGravity";

	public override List<string> Listeners => new List<string>();

	public HighGravity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			ChangePlayerGravity(player, _config.Dices.HighGravity.GravityScale);
			DamageReductionManager.Register(player, "HighGravity", _config.Dices.HighGravity.DamageReduction);
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
		ChangePlayerGravity(player, 1f);
		DamageReductionManager.Unregister(player, "HighGravity");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageReductionManager.Unregister(item, "HighGravity");
			Remove(item);
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private static void ChangePlayerGravity(CCSPlayerController? player, float gravityScale)
	{
		if ((CEntityInstance)(object)((player == null) ? null : ((CBasePlayerController)player).Pawn?.Value) != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			((CBaseEntity)((CBasePlayerController)player).Pawn.Value).ActualGravityScale = gravityScale;
		}
	}
}
