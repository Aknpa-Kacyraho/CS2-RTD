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

/// <summary>
/// 十六夜 Izayoi：周期性进入时间加速，自身获得大幅移速加成。
/// （2026-09-18 重做：原实现用全局 host_timescale 随机加减速，敌我一视同仁、会坑自己；现改为个人时间加速。）
/// </summary>
public class Izayoi : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _nextTriggerTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _speedEndTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Izayoi";

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

	public Izayoi(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_comboActive = DiceSynergy.HasPartner(player, "Heaven");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(player, "Heaven"))
			{
				DiceSynergy.AnnounceCombo(player, "超越天堂", "十六夜与天堂融合！获得超越天堂之力！");
				CCSPlayerController captured = player;
				Server.NextFrame((Action)delegate
				{
					if (instance != null && ((CEntityInstance)captured).IsValid)
					{
						instance.RemoveDiceFromPlayer(captured, "Izayoi");
						instance.RemoveDiceFromPlayer(captured, "Heaven");
						instance.GrantComboDice(captured, "BeyondHeaven");
					}
				});
				return;
			}
			DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！十六夜与天堂共鸣！");
		}
		_nextTriggerTime[player] = Server.CurrentTime + 1.5f;
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		player.PrintToCenterAlert("⏳ 十六夜！时间加速将周期降临！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player != null && player.IsValid)
		{
			SpeedBonusManager.Unregister(player, "IzayoiTime");
		}
		_players.Remove(player);
		_nextTriggerTime.Remove(player);
		_speedEndTime.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (item != null && item.IsValid)
			{
				SpeedBonusManager.Unregister(item, "IzayoiTime");
			}
		}
		_players.Clear();
		_nextTriggerTime.Clear();
		_speedEndTime.Clear();
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
		float duration = _config.Dices.Izayoi.DurationSeconds;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (item == null || !item.IsValid || item.PlayerPawn?.Value == null || !item.PlayerPawn.Value.IsValid)
				{
					continue;
				}
				if (_speedEndTime.TryGetValue(item, out float endTime))
				{
					if (num >= endTime)
					{
						_speedEndTime.Remove(item);
						SpeedBonusManager.Unregister(item, "IzayoiTime");
						item.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(item, 100f);
						Utilities.SetStateChanged(item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
					else
					{
						item.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(item, 100f);
						Utilities.SetStateChanged(item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
				}
				if (_nextTriggerTime.TryGetValue(item, out float next) && num >= next)
				{
					_nextTriggerTime[item] = num + _config.Dices.Izayoi.IntervalSeconds;
					float mult = 1.5f + (float)_random.NextDouble() * 0.7f;
					SpeedBonusManager.Register(item, "IzayoiTime", mult - 1f);
					_speedEndTime[item] = num + duration;
					item.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(item, 100f);
					Utilities.SetStateChanged(item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					item.PrintToCenterAlert($"⏳ 时间加速！移速 ×{mult:F1}，{duration:F0}s");
				}
			}
			catch
			{
			}
		}
	}
}
