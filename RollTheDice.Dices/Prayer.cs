using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Prayer : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _successCount = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _nextPrayTime = new Dictionary<CCSPlayerController, float>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Prayer";

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

	public Prayer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_successCount[player] = 0;
			_nextPrayTime[player] = Server.CurrentTime + _config.Dices.Prayer.PrayInterval;
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
		_successCount.Remove(player);
		_nextPrayTime.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_successCount.Clear();
		_nextPrayTime.Clear();
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
		foreach (CCSPlayerController player in _players.ToList())
		{
			if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || !_nextPrayTime.TryGetValue(player, out var value) || num < value)
			{
				continue;
			}
			_nextPrayTime[player] = num + _config.Dices.Prayer.PrayInterval;
			bool flag = _random.NextDouble() < (double)_config.Dices.Prayer.SuccessChance;
			int valueOrDefault = _successCount.GetValueOrDefault(player, 0);
			CCSPlayerPawn value2 = player.PlayerPawn.Value;
			if (flag)
			{
				valueOrDefault++;
				_successCount[player] = valueOrDefault;
				((CBaseEntity)value2).Health = Math.Min(((CBaseEntity)value2).Health + 50, ((CBaseEntity)value2).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				player.PrintToCenterAlert($"\ud83d\ude4f 祈愿 {valueOrDefault}/{_config.Dices.Prayer.SuccessNeeded} 次成功! +50HP");
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\ude4f {((CBasePlayerController)player).PlayerName} 祈祷成功！+50HP ({valueOrDefault}/{_config.Dices.Prayer.SuccessNeeded})");
				if (valueOrDefault < _config.Dices.Prayer.SuccessNeeded)
				{
					continue;
				}
				foreach (CCSPlayerController item in from e in Utilities.GetPlayers()
					where ((CEntityInstance)e).IsValid && !((CBasePlayerController)e).IsHLTV && ((CBaseEntity)e).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)e.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)e.PlayerPawn.Value).IsValid && ((CBaseEntity)e.PlayerPawn.Value).LifeState == 0
					select e)
				{
					if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
					{
						((CBasePlayerPawn)item.PlayerPawn.Value).CommitSuicide(false, true);
						continue;
					}
					try
					{
						((CBasePlayerPawn)item.PlayerPawn.Value).CommitSuicide(false, true);
					}
					catch
					{
						((CBaseEntity)item.PlayerPawn.Value).Health = 0;
						Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
					}
				}
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc80 {((CBasePlayerController)player).PlayerName} 三次祈愿成功！敌方全灭！");
				_successCount[player] = 0;
				continue;
			}
			((CBaseEntity)value2).Health -= 50;
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)value2).Health <= 0)
			{
				try
				{
					((CBasePlayerPawn)value2).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)value2).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				}
			}
			player.PrintToCenterAlert($"\ud83d\ude4f 祈愿失败... -50HP ({valueOrDefault}/{_config.Dices.Prayer.SuccessNeeded})");
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\ude4f {((CBasePlayerController)player).PlayerName} 祈愿失败... -50HP ({valueOrDefault}/{_config.Dices.Prayer.SuccessNeeded})");
		}
	}
}
