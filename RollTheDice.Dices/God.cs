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
/// 神王 God：整个回合保持 666 生命 / 666 护甲、伤害翻倍并附带减伤；击杀敌人回少量血并获得短暂无敌。
/// （2026-09-18 重做：删除限时试炼，改为稳定形态，避免"不杀人即全损"的净负面体验。）
/// </summary>
public class God : DiceBlueprint
{
	private sealed class GodState
	{
		public int OriginalArmor;

		public int OriginalMaxHealth;
	}

	private readonly Dictionary<CCSPlayerController, GodState> _states = new Dictionary<CCSPlayerController, GodState>();

	public override string ClassName => "God";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public God(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		CCSPlayerPawn pawn = player.PlayerPawn.Value;
		_players.Add(player);
		_states[player] = new GodState
		{
			OriginalArmor = pawn.ArmorValue,
			OriginalMaxHealth = pawn.MaxHealth
		};
		ApplyGodform(player, fullHeal: true);
		DamageBonusManager.Register(player, ClassName, _config.Dices.God.DamageMultiplier - 1f);
		DamageReductionManager.Register(player, ClassName, _config.Dices.God.DamageReduction);
		Invulnerability.Grant(player, _config.Dices.God.InvulnSeconds);
		if (DiceSynergy.HasPartner(player, "Goddess"))
		{
			DiceSynergy.AnnounceCombo(player, "神之共鸣", "神之力回血翻倍！");
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		Restore(player);
		_states.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Restore(player);
		}
		_states.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || !_states.ContainsKey(attacker))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		HealOnKill(attacker);
		Invulnerability.Grant(attacker, _config.Dices.God.InvulnSeconds);
		ApplyGodform(attacker, fullHeal: false);
		return HookResult.Continue;
	}

	private void HealOnKill(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		GodConfig cfg = _config.Dices.God;
		if (cfg.HealOnKill <= 0)
		{
			return;
		}
		int heal = cfg.HealOnKill * (DiceSynergy.HasPartner(player, "Goddess") ? 2 : 1);
		pawn.Health = Math.Min(pawn.Health + heal, pawn.MaxHealth);
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
	}

	/// <param name="fullHeal">首次获得时灌满 666；击杀刷新只维持上限/护甲，不回满。</param>
	private void ApplyGodform(CCSPlayerController player, bool fullHeal)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		GodConfig cfg = _config.Dices.God;
		pawn.MaxHealth = cfg.MaxHealth;
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
		if (fullHeal)
		{
			pawn.Health = cfg.MaxHealth;
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
		}
		pawn.ArmorValue = cfg.ArmorValue;
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
	}

	private void Restore(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || !_states.TryGetValue(player, out GodState state))
		{
			return;
		}
		DamageBonusManager.Unregister(player, ClassName);
		DamageReductionManager.Unregister(player, ClassName);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.MaxHealth = state.OriginalMaxHealth;
			if (pawn.Health > state.OriginalMaxHealth)
			{
				pawn.Health = state.OriginalMaxHealth;
			}
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			pawn.ArmorValue = state.OriginalArmor;
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
		}
	}
}
