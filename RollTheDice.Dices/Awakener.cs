using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 觉醒者 Awakener：每次击杀/助攻成长——+160 生命（抬高上限，可突破原有上限）、+1 倍伤害、+40% 减伤（叠加上限 99%）。
/// 与 Evolution 组合（超进化）：开局即视为已获得 1 次击杀。
/// </summary>
public class Awakener : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _killCount = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Awakener";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Awakener(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_originalMaxHealth[player] = player.PlayerPawn.Value.MaxHealth;
		bool combo = DiceSynergy.HasPartner(player, "Evolution");
		int startKills = combo ? 1 : 0;
		_killCount[player] = startKills;
		ApplyStats(player, startKills);
		if (startKills > 0)
		{
			GrantHealth(player, startKills);
			DiceSynergy.AnnounceCombo(player, "超进化", "超进化联动生效！初始+1击杀！");
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
		player.PrintToCenterAlert("⚡ 觉醒者！每次击杀/助攻：+160HP、伤害+100%、减伤+40%");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		Revert(player);
		_players.Remove(player);
		_killCount.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Revert(player);
		}
		_players.Clear();
		_killCount.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void Revert(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			if (player != null)
			{
				_originalMaxHealth.Remove(player);
			}
			return;
		}
		DamageBonusManager.Unregister(player, ClassName);
		DamageReductionManager.Unregister(player, ClassName);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid && _originalMaxHealth.TryGetValue(player, out int original))
		{
			pawn.MaxHealth = original;
			if (pawn.Health > original)
			{
				pawn.Health = original;
			}
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
		}
		_originalMaxHealth.Remove(player);
	}

	private void ApplyStats(CCSPlayerController player, int kills)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		AwakenerConfig cfg = _config.Dices.Awakener;
		float damageBonus = kills * cfg.DamageBonusPerKill;
		float reduction = Math.Min(kills * cfg.ReductionPerKill, cfg.ReductionCap);
		DamageBonusManager.Register(player, ClassName, damageBonus);
		DamageReductionManager.Register(player, ClassName, reduction, cfg.ReductionCap);
	}

	private void GrantHealth(CCSPlayerController player, int kills)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || kills <= 0)
		{
			return;
		}
		int gain = kills * _config.Dices.Awakener.HpPerKill;
		pawn.MaxHealth += gain;
		pawn.Health += gain;
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		if (victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		CheckKill(@event.Attacker, victim);
		CheckKill(@event.Assister, victim);
		return HookResult.Continue;
	}

	private void CheckKill(CCSPlayerController? player, CCSPlayerController victim)
	{
		if (player == null || !player.IsValid || !_players.Contains(player))
		{
			return;
		}
		// 只有击杀/助攻敌人（不同队）才计入成长，避免刷队友或自杀。
		if (((CBaseEntity)player).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return;
		}
		int kills = (_killCount.TryGetValue(player, out int value) ? value : 0) + 1;
		_killCount[player] = kills;
		ApplyStats(player, kills);
		GrantHealth(player, 1);
		AwakenerConfig cfg = _config.Dices.Awakener;
		float reduction = Math.Min(kills * cfg.ReductionPerKill, cfg.ReductionCap);
		player.PrintToCenterAlert($"⚡ 觉醒！+{cfg.HpPerKill}HP，伤害+{kills * cfg.DamageBonusPerKill * 100f:0}%，减伤+{reduction * 100f:0}%");
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
			{
				continue;
			}
			if (player.PlayerPawn.Value.LifeState == 0)
			{
				ApplyStats(player, _killCount.TryGetValue(player, out int kills) ? kills : 0);
			}
		}
	}
}
