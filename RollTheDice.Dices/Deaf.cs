using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 失聪 Deaf：完全听不到声音；受到伤害时透视攻击者一段时间作为补偿。
/// </summary>
public class Deaf : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, (CDynamicProp Proxy, CDynamicProp Glow, CCSPlayerController Target)> _activeGlows = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp, CCSPlayerController)>();

	public override string ClassName => "Deaf";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public override Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode>
	{
		{
			208,
			HookMode.Pre
		}
	};

	public Deaf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		_players.Add(player);
		player.ExecuteClientCommand("volume 0");
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
		player.PrintToCenterAlert("🔇 完全失聪！受到伤害时透视攻击者");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		RemoveGlowForPlayer(player);
		if (player != null)
		{
			player.ExecuteClientCommand("volume 0.5");
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (player != null)
			{
				player.ExecuteClientCommand("volume 0.5");
			}
			RemoveGlowForPlayer(player);
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (victim == null || !victim.IsValid || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		if (attacker == null || !attacker.IsValid || attacker == victim)
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn attackerPawn = attacker.PlayerPawn?.Value;
		if (attackerPawn == null || !attackerPawn.IsValid)
		{
			return HookResult.Continue;
		}
		RemoveGlowForPlayer(victim);
		(CDynamicProp Proxy, CDynamicProp Glow) glow = GlowUtil.CreateGlow(attackerPawn, Color.Red);
		if (glow.Proxy == null || glow.Glow == null)
		{
			return HookResult.Continue;
		}
		_activeGlows[victim] = (glow.Proxy, glow.Glow, attacker);
		float seconds = _config.Dices.Deaf.RevealSeconds;
		victim.PrintToCenterAlert($"👁 透视 {((CBasePlayerController)attacker).PlayerName}！{seconds:F0}s");
		new Timer(seconds, delegate
		{
			RemoveGlowForPlayer(victim);
		}, (TimerFlags?)null);
		return HookResult.Continue;
	}

	public HookResult HookUserMessage208(UserMessage um)
	{
		if (_players.Count == 0)
		{
			return HookResult.Continue;
		}
		int sourceIndex = um.ReadInt("source_entity_index", null);
		foreach (CCSPlayerController player in _players)
		{
			if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.Index == sourceIndex)
			{
				um.Recipients.Clear();
				return HookResult.Stop;
			}
		}
		return HookResult.Continue;
	}

	private void RemoveGlowForPlayer(CCSPlayerController holder)
	{
		if (holder != null && _activeGlows.TryGetValue(holder, out (CDynamicProp, CDynamicProp, CCSPlayerController) value))
		{
			GlowUtil.RemoveGlow(value.Item1, value.Item2);
			_activeGlows.Remove(holder);
		}
	}
}
