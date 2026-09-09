using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Bank : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private float _nextPayout;

	public override string ClassName => "Bank";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
			return list;
		}
	}

	public Bank(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Miser");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "资本要塞", "资本要塞联动生效！");
			}
			if (_nextPayout == 0f)
			{
				_nextPayout = Server.CurrentTime + _config.Dices.Bank.Interval;
			}
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
		_nextPayout = 0f;
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
		float num = Server.CurrentTime;
		if (num < _nextPayout)
		{
			return;
		}
		_nextPayout = num + _config.Dices.Bank.Interval;
		int minAmount = _config.Dices.Bank.MinAmount;
		int maxAmount = _config.Dices.Bank.MaxAmount;
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if ((CEntityInstance)(object)holder == (CEntityInstance)null || !((CEntityInstance)holder).IsValid)
			{
				continue;
			}
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum == ((CBaseEntity)holder).TeamNum && (CEntityInstance)(object)p != (CEntityInstance)(object)holder && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p).ToList();
			if (list.Count == 0)
			{
				continue;
			}
			int num2 = Math.Min(_config.Dices.Bank.TeammatesCount, list.Count);
			HashSet<CCSPlayerController> hashSet = new HashSet<CCSPlayerController>();
			for (int num3 = 0; num3 < num2; num3++)
			{
				int num4 = 0;
				CCSPlayerController val;
				do
				{
					val = list[_random.Next(list.Count)];
					num4++;
				}
				while (hashSet.Contains(val) && num4 < 20);
				if (!hashSet.Contains(val))
				{
					hashSet.Add(val);
					int num5 = (DiceSynergy.HasPartner(holder, "Miser") ? (_random.Next(minAmount, maxAmount + 1) * 2) : _random.Next(minAmount, maxAmount + 1));
					val.InGameMoneyServices.Account += num5;
					if (val.InGameMoneyServices.Account < 0)
					{
						val.InGameMoneyServices.Account = 0;
					}
					Utilities.SetStateChanged((CBaseEntity)(object)val, "CCSPlayerController", "m_pInGameMoneyServices", 0);
					val.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Bank_payout"].Value.Replace("{amount}", ((num5 >= 0) ? "+" : "") + num5).Replace("{name}", ((CBasePlayerController)val).PlayerName));
				}
			}
		}
	}
}
