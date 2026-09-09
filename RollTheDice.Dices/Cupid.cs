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

public class Cupid : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private bool _processingDeath;

	public override string ClassName => "Cupid";

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

	public Cupid(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
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

	public override void Reset()
	{
		_players.Clear();
		_processingDeath = false;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (_processingDeath)
		{
			return (HookResult)0;
		}
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		_processingDeath = true;
		float delay = _config.Dices.Cupid.DeathDelay;
		CCSPlayerController diceOwner = userid;
		Server.NextFrame((Action)delegate
		{
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)diceOwner && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0
				select p).ToList();
			if (list.Count != 0)
			{
				CCSPlayerController target = list[_random.Next(list.Count)];
				new Timer(delay, (Action)delegate
				{
					if ((CEntityInstance)(object)target == (CEntityInstance)null || !((CEntityInstance)target).IsValid || (CEntityInstance)(object)target.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)target.PlayerPawn.Value).IsValid || ((CBaseEntity)target.PlayerPawn.Value).LifeState != 0)
					{
						_processingDeath = false;
					}
					else
					{
						if (!target.IsBot)
						{
							((CBasePlayerPawn)target.PlayerPawn.Value).CommitSuicide(false, true);
						}
						target.PrintToCenterAlert("\ud83d\udc98 丘比特之箭射中了你!");
						_processingDeath = false;
					}
				}, (TimerFlags?)null);
			}
			else
			{
				_processingDeath = false;
			}
		});
		return (HookResult)0;
	}
}
