using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 愚者 Fool：攻击有概率失效；被攻击有概率获得短暂无敌。
/// 连败保底：连续若干次攻击失效后，下一次必定命中。
/// </summary>
public class Fool : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<ulong, int> _consecutiveWhiffs = new Dictionary<ulong, int>();

	public override string ClassName => "Fool";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public Fool(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_consecutiveWhiffs[player.SteamID] = 0;
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
		_consecutiveWhiffs.Remove(player.SteamID);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_consecutiveWhiffs.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info == null || info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		CCSPlayerController victim = ResolvePlayer(entity);
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		FoolConfig cfg = _config.Dices.Fool;
		bool changed = false;
		if (attacker != null && attacker.IsValid && _players.Contains(attacker))
		{
			ulong attackerId = attacker.SteamID;
			int whiffs = _consecutiveWhiffs.TryGetValue(attackerId, out int w) ? w : 0;
			if (whiffs >= cfg.WhiffPity)
			{
				_consecutiveWhiffs[attackerId] = 0;
			}
			else if (_random.NextDouble() < cfg.AttackWhiffChance)
			{
				_consecutiveWhiffs[attackerId] = whiffs + 1;
				info.Damage = 0f;
				changed = true;
			}
			else
			{
				_consecutiveWhiffs[attackerId] = 0;
			}
		}
		if (info.Damage > 0f && victim != null && victim.IsValid && _players.Contains(victim) && !Invulnerability.IsInvulnerable(victim) && _random.NextDouble() < cfg.InvincibilityChance)
		{
			Invulnerability.Grant(victim, cfg.InvincibilitySeconds);
			info.Damage = 0f;
			victim.PrintToCenterAlert($"🎴 愚者庇护！无敌 {cfg.InvincibilitySeconds:F0} 秒！");
			changed = true;
		}
		return changed ? HookResult.Changed : HookResult.Continue;
	}

	private static CCSPlayerController ResolvePlayer(CBaseEntity entity)
	{
		if (entity == null)
		{
			return null;
		}
		CCSPlayerPawn pawn = entity.As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return null;
		}
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null || controller.Value == null)
		{
			return null;
		}
		return controller.Value.As<CCSPlayerController>();
	}
}
