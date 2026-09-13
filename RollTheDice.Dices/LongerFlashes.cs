using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 闪光大师 LongerFlashes：你的闪光致盲时间延长；你被闪光后 3 秒内移速 +30%（被闪也能反打）。
/// </summary>
public class LongerFlashes : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "LongerFlashes";

	public override List<string> Events => new List<string> { "EventPlayerBlind" };

	public LongerFlashes(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		SpeedBonusManager.Unregister(player, ClassName);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			SpeedBonusManager.Unregister(player, ClassName);
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker != null && attacker.IsValid && _players.Contains(attacker))
		{
			float factor = (float)(_random.NextDouble() * (_config.Dices.LongerFlashes.MaxBlinddurationFactor - _config.Dices.LongerFlashes.MinBlinddurationFactor) + _config.Dices.LongerFlashes.MinBlinddurationFactor);
			@event.BlindDuration *= factor;
			CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
			if (victimPawn != null && victimPawn.IsValid)
			{
				((CCSPlayerPawnBase)victimPawn).FlashDuration = @event.BlindDuration;
				Utilities.SetStateChanged(victimPawn, "CCSPlayerPawnBase", "m_flFlashDuration", 0);
				((CCSPlayerPawnBase)victimPawn).BlindUntilTime = Server.CurrentTime + ((CCSPlayerPawnBase)victimPawn).FlashDuration;
			}
		}
		if (_players.Contains(victim))
		{
			GrantSelfSpeed(victim);
		}
		return HookResult.Continue;
	}

	private void GrantSelfSpeed(CCSPlayerController player)
	{
		float seconds = _config.Dices.LongerFlashes.SelfSpeedSeconds;
		SpeedBonusManager.Register(player, ClassName, _config.Dices.LongerFlashes.SelfSpeedBonus, seconds);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
		ulong id = player.SteamID;
		new Timer(seconds, delegate
		{
			SpeedBonusManager.UnregisterBySteamId(id, ClassName);
			CCSPlayerController target = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && ((CBasePlayerController)p).SteamID == id);
			CCSPlayerPawn targetPawn = target?.PlayerPawn?.Value;
			if (targetPawn != null && targetPawn.IsValid)
			{
				targetPawn.VelocityModifier = 1f + SpeedBonusManager.GetEffectiveBySteamId(id, 100f);
				Utilities.SetStateChanged(targetPawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}, (TimerFlags?)null);
	}
}
