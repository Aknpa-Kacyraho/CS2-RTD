using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 沙漠之鹰 DeagleKing：沙鹰伤害×3；沙鹰爆头击杀回复 50 HP（借 StackingHealth 提升上限，可超过 100）。
/// </summary>
public class DeagleKing : DiceBlueprint
{
	private const string WeaponMark = "deagle";

	public override string ClassName => "DeagleKing";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public DeagleKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		StackingHealth.RegisterMultiplier(player, ClassName, _config.Dices.DeagleKing.MaxHealthMultiplier);
		if (DiceSynergy.HasPartner(player, "SniperElite"))
		{
			DiceSynergy.AnnounceCombo(player, "精准猎杀", "沙鹰伤害提升至 5 倍！");
		}
		if (DiceSynergy.HasPartner(player, "DeadHand"))
		{
			DiceSynergy.AnnounceCombo(player, "致命一击", "沙鹰伤害提升至 5 倍！");
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
		StackingHealth.UnregisterMultiplier(player, ClassName);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players)
		{
			StackingHealth.UnregisterMultiplier(player, ClassName);
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
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		if (attacker == null || !attacker.IsValid || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		CBasePlayerWeapon weapon = GetActiveWeapon(attacker);
		if (weapon == null || !weapon.IsValid)
		{
			return HookResult.Continue;
		}
		string name = weapon.DesignerName;
		if (name == null || !name.Contains(WeaponMark))
		{
			return HookResult.Continue;
		}
		bool combo = DiceSynergy.HasPartner(attacker, "SniperElite") || DiceSynergy.HasPartner(attacker, "DeadHand");
		info.Damage *= combo ? 5f : _config.Dices.DeagleKing.DamageMultiplier;
		return HookResult.Changed;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
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
		if (!@event.Headshot)
		{
			return HookResult.Continue;
		}
		string weapon = @event.Weapon;
		if (weapon == null || !weapon.Contains(WeaponMark))
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn pawn = attacker.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return HookResult.Continue;
		}
		CBaseEntity entity = pawn;
		entity.Health = Math.Min(entity.Health + _config.Dices.DeagleKing.HeadshotHeal, entity.MaxHealth);
		Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
		attacker.PrintToCenterAlert($"沙鹰爆头击杀！+{_config.Dices.DeagleKing.HeadshotHeal} HP");
		return HookResult.Continue;
	}

	private static CBasePlayerWeapon GetActiveWeapon(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		return pawn?.WeaponServices?.ActiveWeapon?.Value;
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
