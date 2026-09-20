using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 十六夜 Izayoi：周期扰动全局时间造成全场减速，但持有者自身移速不受影响（速度×timescale 恒为自身基准值）。
/// （2026-09-19 重做：恢复"时间扰动"主题，但只减速不加速；持有者用 VelocityModifier 补偿全局 timescale。）
/// </summary>
public class Izayoi : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _nextTriggerTime = new Dictionary<CCSPlayerController, float>();

	/// <summary>全局减速结束时间（0=未激活）。</summary>
	private float _slowEndTime;

	private float _slowFactor = 1f;

	private bool _slowActive;

	public override string ClassName => "Izayoi";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Izayoi(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
					if (instance != null && captured != null && captured.IsValid)
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
		player.PrintToCenterAlert("⏳ 十六夜！时间扰动将周期降临！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player != null && player.IsValid)
		{
			RestoreHolderSpeed(player);
		}
		_players.Remove(player);
		_nextTriggerTime.Remove(player);
		if (_players.Count == 0)
		{
			StopSlow();
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			RestoreHolderSpeed(item);
		}
		_players.Clear();
		_nextTriggerTime.Clear();
		StopSlow();
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
		float now = Server.CurrentTime;

		if (_slowActive && now >= _slowEndTime)
		{
			StopSlow();
		}

		foreach (CCSPlayerController item in _players.ToList())
		{
			if (item == null || !item.IsValid || item.PlayerPawn?.Value == null || !item.PlayerPawn.Value.IsValid)
			{
				continue;
			}
			if (_nextTriggerTime.TryGetValue(item, out float next) && now >= next)
			{
				_nextTriggerTime[item] = now + _config.Dices.Izayoi.IntervalSeconds;
				float min = _config.Dices.Izayoi.MinFactor;
				float max = _config.Dices.Izayoi.MaxFactor;
				if (max < min)
				{
					(min, max) = (max, min);
				}
				float factor = min + (float)_random.NextDouble() * (max - min);
				factor = Math.Clamp(factor, 0.1f, 0.99f);
				// 多个持有者同时扰动时，取最慢（最有利于持有者）。
				_slowFactor = _slowActive ? Math.Min(_slowFactor, factor) : factor;
				_slowEndTime = now + _config.Dices.Izayoi.DurationSeconds;
				_slowActive = true;
				Server.ExecuteCommand("host_timescale " + _slowFactor.ToString("0.00", CultureInfo.InvariantCulture));
				Server.PrintToChatAll($"⏳ 时间被扰动了！全场减速 {_slowFactor:0.00}x");
				item.PrintToCenterAlert($"⏳ 时间扰动！全场减速 {_slowFactor:0.00}x");
			}
			if (_slowActive)
			{
				ApplyHolderSpeed(item);
			}
		}
	}

	/// <summary>持有者速度补偿：全局 timescale=f 时，把自身 VelocityModifier 提到 (基准/f)，保持实际移速不变。</summary>
	private void ApplyHolderSpeed(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		float bonus = SpeedBonusManager.GetEffective(player, 100f);
		pawn.VelocityModifier = (1f + bonus) / _slowFactor;
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
	}

	private void RestoreHolderSpeed(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
	}

	private void StopSlow()
	{
		if (!_slowActive)
		{
			return;
		}
		_slowActive = false;
		_slowFactor = 1f;
		Server.ExecuteCommand("host_timescale 1.0");
		foreach (CCSPlayerController item in _players.ToList())
		{
			RestoreHolderSpeed(item);
		}
	}
}
