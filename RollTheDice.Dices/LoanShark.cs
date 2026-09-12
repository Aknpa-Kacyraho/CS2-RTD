using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class LoanShark : DiceBlueprint
{
	private readonly HashSet<ulong> _hasKilledThisRound = new HashSet<ulong>();

	private readonly HashSet<ulong> _loanActive = new HashSet<ulong>();

	public override string ClassName => "LoanShark";

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

	public LoanShark(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_loanActive.Add(((CBasePlayerController)player).SteamID);
			int loanAmount = _config.Dices.LoanShark.LoanAmount;
			player.InGameMoneyServices.Account = loanAmount;
			Utilities.SetStateChanged((CBaseEntity)(object)player, "CCSPlayerController", "m_pInGameMoneyServices", 0);
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
		foreach (ulong sid in _loanActive.ToList())
		{
			CCSPlayerController item = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == sid);
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || item.InGameMoneyServices == null)
			{
				continue;
			}
			item.InGameMoneyServices.Account = 0;
			Utilities.SetStateChanged((CBaseEntity)(object)item, "CCSPlayerController", "m_pInGameMoneyServices", 0);
			if (_hasKilledThisRound.Contains(sid))
			{
				continue;
			}
			item.PrintToCenterAlert("你没能还债！高利贷找上门了！");
			CCSPlayerPawn pawn = item.PlayerPawn?.Value;
			if ((CEntityInstance)(object)pawn == (CEntityInstance)null || !((CEntityInstance)pawn).IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
			{
				((CBasePlayerPawn)pawn).CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerPawn)pawn).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)pawn).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iHealth", 0);
			}
		}
		_players.Clear();
		_loanActive.Clear();
		_hasKilledThisRound.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)@event.Userid)
		{
			return (HookResult)0;
		}
		if (_loanActive.Contains(((CBasePlayerController)attacker).SteamID))
		{
			_hasKilledThisRound.Add(((CBasePlayerController)attacker).SteamID);
		}
		return (HookResult)0;
	}
}
