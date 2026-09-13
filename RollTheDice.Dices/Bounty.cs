using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Bounty : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, ulong> _bountyTargets = new Dictionary<CCSPlayerController, ulong>();

	private readonly List<Timer> _pendingTimers = new List<Timer>();

	private bool _comboActive;

	private static readonly string[] _rewardPool = new string[5] { "Amber", "Adrenaline", "DeagleKing", "PoisonBlade", "ToxicSmoke" };

	public override string ClassName => "Bounty";

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

	public Bounty(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Expected O, but got Unknown
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		_comboActive = DiceSynergy.HasPartner(player, "Capitalist");
		if (_comboActive)
		{
			DiceSynergy.AnnounceCombo(player, "赏金猎人", "赏金加倍！");
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		Timer item = new Timer(5f, (Action)delegate
		{
			if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _players.Contains(player))
			{
				AssignBountyTarget(player);
			}
		}, (TimerFlags?)null);
		_pendingTimers.Add(item);
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_bountyTargets.Remove(player);
	}

	public override void Reset()
	{
		foreach (Timer pendingTimer in _pendingTimers)
		{
			pendingTimer.Kill();
		}
		_pendingTimers.Clear();
		_players.Clear();
		_bountyTargets.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void AssignBountyTarget(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid)
		{
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p).ToList();
			if (list.Count != 0)
			{
				CCSPlayerController val = list[_random.Next(list.Count)];
				_bountyTargets[player] = ((CBasePlayerController)val).SteamID;
				player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83c\udfaf 赏金目标：{((CBasePlayerController)val).PlayerName}！击杀获得额外随机骰子！");
			}
		}
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if (attacker.InGameMoneyServices != null)
		{
			int num = _random.Next(_config.Dices.Bounty.MoneyMin, _config.Dices.Bounty.MoneyMax + 1);
			if (DiceSynergy.HasPartner(attacker, "Capitalist"))
			{
				num *= 2;
			}
			attacker.InGameMoneyServices.Account += num;
			Utilities.SetStateChanged((CBaseEntity)(object)attacker, "CCSPlayerController", "m_pInGameMoneyServices", 0);
			attacker.PrintToCenterAlert($"\ud83d\udcb0 赏金 +${num}!");
		}
		if (_bountyTargets.TryGetValue(attacker, out var value) && value == ((CBasePlayerController)userid).SteamID)
		{
			string text = _rewardPool[_random.Next(_rewardPool.Length)];
			string value2 = _localizer["dice_" + text + "_name"].Value;
			RollTheDice.Instance?.ForceDiceForPlayer(attacker, text);
			attacker.PrintToChat($" {_localizer["command.prefix"].Value}\ud83c\udfaf 击杀赏金目标！额外获得骰子：{value2}！");
			_bountyTargets.Remove(attacker);
		}
		return (HookResult)0;
	}
}
