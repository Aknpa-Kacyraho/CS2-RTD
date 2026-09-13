using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 蚊子 Mosquito：体型极小、生命极低；攻击命中使敌人减速。
/// 与 PlayAsChicken 组合（迷你鸡神）。
/// </summary>
public class Mosquito : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<ulong, float> _slowUntil = new Dictionary<ulong, float>();

	public override string ClassName => "Mosquito";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public Mosquito(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_originalMaxHealth[player] = ((CBaseEntity)pawn).MaxHealth;
		float scale = _config.Dices.Mosquito.SizeScale;
		((CGameSceneNode)((CBaseEntity)pawn).CBodyComponent.SceneNode.GetSkeletonInstance()).Scale = scale;
		pawn.AcceptInput("SetScale", null, null, scale.ToString(), 0);
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent", 0);
		int health = _config.Dices.Mosquito.Health;
		((CBaseEntity)pawn).MaxHealth = health;
		((CBaseEntity)pawn).Health = health;
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
		_players.Add(player);
		if (DiceSynergy.HasPartner(player, "PlayAsChicken"))
		{
			DiceSynergy.AnnounceCombo(player, "迷你鸡神", "蚊子叮咬更痛！");
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
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			((CGameSceneNode)((CBaseEntity)pawn).CBodyComponent.SceneNode.GetSkeletonInstance()).Scale = 1f;
			pawn.AcceptInput("SetScale", null, null, "1", 0);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent", 0);
			if (_originalMaxHealth.TryGetValue(player, out int original))
			{
				((CBaseEntity)pawn).MaxHealth = original;
				if (((CBaseEntity)pawn).Health > original)
				{
					((CBaseEntity)pawn).Health = original;
				}
				Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			}
		}
		_originalMaxHealth.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Remove(player);
		}
		_originalMaxHealth.Clear();
		_slowUntil.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (victimPawn == null || !victimPawn.IsValid)
		{
			return HookResult.Continue;
		}
		MosquitoConfig cfg = _config.Dices.Mosquito;
		ulong id = victim.SteamID;
		_slowUntil[id] = Server.CurrentTime + cfg.SlowSeconds;
		SpeedBonusManager.Register(victim, ClassName, -cfg.Slow);
		victimPawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(victim, 100f);
		Utilities.SetStateChanged(victimPawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		attacker.PrintToCenterAlert("蚊子叮咬！敌人被减速");
		new Timer(cfg.SlowSeconds, delegate
		{
			RestoreSlow(id);
		}, (TimerFlags?)null);
		return HookResult.Continue;
	}

	private void RestoreSlow(ulong steamId)
	{
		if (_slowUntil.TryGetValue(steamId, out float until) && Server.CurrentTime < until - 0.05f)
		{
			return;
		}
		_slowUntil.Remove(steamId);
		SpeedBonusManager.UnregisterBySteamId(steamId, ClassName);
		CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && ((CBasePlayerController)p).SteamID == steamId);
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffectiveBySteamId(steamId, 100f);
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}
}
