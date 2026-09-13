using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class DivinePunishment : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _killCounts = new Dictionary<CCSPlayerController, int>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "DivinePunishment";

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

	public DivinePunishment(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_killCounts[player] = 0;
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
		_killCounts.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_killCounts.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker == (CEntityInstance)(object)@event.Userid)
		{
			return (HookResult)0;
		}
		int num = (_killCounts.TryGetValue(attacker, out var value) ? value : 0);
		num++;
		_killCounts[attacker] = num;
		int killsRequired = _config.Dices.DivinePunishment.KillsRequired;
		if (num < killsRequired)
		{
			attacker.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_DivinePunishment_progress"].Value.Replace("{current}", num.ToString()).Replace("{required}", killsRequired.ToString()));
			return (HookResult)0;
		}
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)attacker).TeamNum && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0
			select p).ToList();
		if (list.Count == 0)
		{
			return (HookResult)0;
		}
		_killCounts[attacker] = 0;
		CCSPlayerController val = list[_random.Next(list.Count)];
		CCSPlayerController capturedTarget = val;
		string attackerName = ((CBasePlayerController)attacker).PlayerName;
		Server.NextFrame((Action)delegate
		{
			if (!((CEntityInstance)(object)capturedTarget == (CEntityInstance)null) && ((CEntityInstance)capturedTarget).IsValid && !((CEntityInstance)(object)capturedTarget.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)capturedTarget.PlayerPawn.Value).IsValid && ((CBaseEntity)capturedTarget.PlayerPawn.Value).LifeState == 0)
			{
				if (!capturedTarget.IsBot && !((CBasePlayerController)capturedTarget).IsHLTV)
				{
					((CBasePlayerPawn)capturedTarget.PlayerPawn.Value).CommitSuicide(false, true);
				}
				else
				{
					try
					{
						((CBasePlayerPawn)capturedTarget.PlayerPawn.Value).CommitSuicide(false, true);
					}
					catch
					{
						((CBaseEntity)capturedTarget.PlayerPawn.Value).Health = 0;
						Utilities.SetStateChanged((CBaseEntity)(object)capturedTarget.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
					}
				}
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_DivinePunishment_trigger"].Value.Replace("{attacker}", attackerName).Replace("{target}", ((CBasePlayerController)capturedTarget).PlayerName));
			}
		});
		return (HookResult)0;
	}
}
