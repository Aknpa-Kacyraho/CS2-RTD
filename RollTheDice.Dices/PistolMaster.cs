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
/// 手枪大师 PistolMaster：手枪伤害 +100%；手枪击杀 +$300（ECO 回合经济）。
/// 与 Disarm 组合（缴械大师）：伤害进一步提升。
/// </summary>
public class PistolMaster : DiceBlueprint
{
	public override string ClassName => "PistolMaster";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public PistolMaster(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if (DiceSynergy.HasPartner(player, "Disarm"))
		{
			DiceSynergy.AnnounceCombo(player, "缴械大师", "手枪伤害与缴械概率提升！");
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
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
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
		CBasePlayerWeapon weapon = attacker.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value;
		if (weapon == null || !weapon.IsValid || !IsPistol(weapon.DesignerName))
		{
			return HookResult.Continue;
		}
		float multiplier = _config.Dices.PistolMaster.DamageMultiplier;
		if (DiceSynergy.HasPartner(attacker, "Disarm"))
		{
			multiplier += 1f;
		}
		info.Damage *= multiplier;
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
		string weapon = @event.Weapon;
		if (weapon == null || !IsPistol(weapon))
		{
			return HookResult.Continue;
		}
		if (attacker.InGameMoneyServices == null)
		{
			return HookResult.Continue;
		}
		attacker.InGameMoneyServices.Account += _config.Dices.PistolMaster.KillReward;
		Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pInGameMoneyServices", 0);
		attacker.PrintToCenterAlert($"手枪击杀 +${_config.Dices.PistolMaster.KillReward}");
		return HookResult.Continue;
	}

	private static bool IsPistol(string designerName)
	{
		if (string.IsNullOrEmpty(designerName))
		{
			return false;
		}
		string name = designerName.ToLower();
		bool pistol = name.Contains("pistol") || name.Contains("deagle") || name.Contains("elite");
		bool revolver = name.Contains("revolver");
		if (revolver && !name.Contains("elite"))
		{
			return false;
		}
		return pistol && !revolver;
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
