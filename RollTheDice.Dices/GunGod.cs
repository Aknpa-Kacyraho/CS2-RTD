using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 枪神 GunGod：免疫投掷物/燃烧/刀伤；每击杀叠加减伤（上限 max_reduction），自身死亡清零。
/// 与 NoRecoil 组合（完美枪械）。
/// </summary>
public class GunGod : DiceBlueprint
{
	private static readonly HashSet<string> GrenadeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"hegrenade_projectile",
		"flashbang_projectile",
		"smokegrenade_projectile",
		"molotov_projectile",
		"incendiarygrenade_projectile",
		"decoy_projectile"
	};

	public override string ClassName => "GunGod";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public GunGod(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		DamageReductionManager.Register(player, ClassName, 0f, _config.Dices.GunGod.MaxReduction);
		if (DiceSynergy.HasPartner(player, "NoRecoil"))
		{
			DiceSynergy.AnnounceCombo(player, "完美枪械", "减伤上限提高，任意武器零扩散！");
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
		DamageReductionManager.Unregister(player, ClassName);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players)
		{
			DamageReductionManager.Unregister(player, ClassName);
		}
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
		if (victim == null || !victim.IsValid || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		string inflictorName = info.Inflictor?.Value?.DesignerName;
		if (inflictorName != null && GrenadeTypes.Contains(inflictorName))
		{
			info.Damage = 0f;
			return HookResult.Changed;
		}
		if (((uint)info.BitsDamageType & 8u) != 0 || ((uint)info.BitsDamageType & 4u) != 0)
		{
			info.Damage = 0f;
			return HookResult.Changed;
		}
		return HookResult.Continue;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (victim != null && victim.IsValid && _players.Contains(victim))
		{
			DamageReductionManager.Register(victim, ClassName, 0f, _config.Dices.GunGod.MaxReduction);
		}
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
		GunGodConfig cfg = _config.Dices.GunGod;
		float current = DamageReductionManager.GetSource(attacker, ClassName);
		float next = Math.Min(current + cfg.PerKill, cfg.MaxReduction);
		DamageReductionManager.Register(attacker, ClassName, next, cfg.MaxReduction);
		attacker.PrintToCenterAlert($"枪神减伤 {next * 100f:F0}%");
		return HookResult.Continue;
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
