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
/// 神王 God：神之试炼——获得高额移速/伤害/护甲，但限时内未击杀则失去全部；每次击杀刷新时限。
/// 与 Goddess 组合（神之共鸣）。
/// </summary>
public class God : DiceBlueprint
{
	private sealed class TrialState
	{
		public int OriginalArmor;

		public float Deadline;

		public bool Active;
	}

	private readonly Dictionary<CCSPlayerController, TrialState> _states = new Dictionary<CCSPlayerController, TrialState>();

	public override string ClassName => "God";

	public override List<string> Listeners => new List<string> { "OnTick" };

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
		_players.Add(player);
		_states[player] = new TrialState
		{
			OriginalArmor = player.PlayerPawn.Value.ArmorValue
		};
		Activate(player);
		if (DiceSynergy.HasPartner(player, "Goddess"))
		{
			DiceSynergy.AnnounceCombo(player, "神之共鸣", "神之力时限延长！");
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
		Expire(player);
		_states.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Expire(player);
		}
		_states.Clear();
		_players.Clear();
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
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (!_states.TryGetValue(player, out TrialState state) || !state.Active)
			{
				continue;
			}
			if (now >= state.Deadline)
			{
				Expire(player);
				player?.PrintToCenterAlert("神之试炼失败！失去全部神力");
			}
		}
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
		Activate(attacker);
		HealOnKill(attacker);
		return HookResult.Continue;
	}

	private void HealOnKill(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		int heal = _config.Dices.God.HealOnKill;
		if (heal > 0)
		{
			pawn.Health = Math.Min(pawn.Health + heal, pawn.MaxHealth);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
		}
	}

	private void Activate(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || !_states.TryGetValue(player, out TrialState state))
		{
			return;
		}
		GodConfig cfg = _config.Dices.God;
		float duration = DiceSynergy.HasPartner(player, "Goddess") ? cfg.Duration * 1.5f : cfg.Duration;
		state.Deadline = Server.CurrentTime + duration;
		state.Active = true;
		SpeedBonusManager.Register(player, ClassName, cfg.SpeedMultiplier - 1f);
		DamageBonusManager.Register(player, ClassName, cfg.DamageMultiplier - 1f);
		DamageReductionManager.Register(player, ClassName, cfg.DamageReduction);
		Invulnerability.Grant(player, cfg.InvulnSeconds);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.ArmorValue = state.OriginalArmor + cfg.ArmorBonus;
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private void Expire(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || !_states.TryGetValue(player, out TrialState state))
		{
			return;
		}
		state.Active = false;
		SpeedBonusManager.Unregister(player, ClassName);
		DamageBonusManager.Unregister(player, ClassName);
		DamageReductionManager.Unregister(player, ClassName);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			if (pawn.ArmorValue > state.OriginalArmor)
			{
				pawn.ArmorValue = state.OriginalArmor;
				Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
			}
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}
}
