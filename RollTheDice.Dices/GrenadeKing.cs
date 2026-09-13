using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 手雷王 GrenadeKing：手雷伤害 ×2.5、爆炸范围 +25%；手雷击杀返还一颗手雷。
/// 与 Martyrdom 组合（爆炸艺术家）：伤害与范围进一步提升。
/// </summary>
public class GrenadeKing : DiceBlueprint
{
	private readonly Dictionary<uint, float> _nadeMultiplier = new Dictionary<uint, float>();

	public override string ClassName => "GrenadeKing";

	public override List<string> Listeners => new List<string> { "OnEntitySpawned", "OnPlayerTakeDamagePre" };

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public GrenadeKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if (DiceSynergy.HasPartner(player, "Martyrdom"))
		{
			DiceSynergy.AnnounceCombo(player, "爆炸艺术家", "手雷伤害与爆炸范围大幅提升！");
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
		_nadeMultiplier.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count == 0 || entity == null || entity.DesignerName != "hegrenade_projectile")
		{
			return;
		}
		Server.NextFrame(delegate
		{
			if (entity == null || !entity.IsValid)
			{
				return;
			}
			CHEGrenadeProjectile projectile = new CHEGrenadeProjectile(entity.Handle);
			if (!projectile.IsValid)
			{
				return;
			}
			CCSPlayerPawn thrower = ((CBaseGrenade)projectile).Thrower?.Value;
			if (thrower == null || !thrower.IsValid || thrower.Controller?.Value == null)
			{
				return;
			}
			CCSPlayerController owner = thrower.Controller.Value.As<CCSPlayerController>();
			if (owner == null || !owner.IsValid || !_players.Contains(owner))
			{
				return;
			}
			float multiplier = _config.Dices.GrenadeKing.DamageMultiplier;
			float radius = _config.Dices.GrenadeKing.RadiusMultiplier;
			if (DiceSynergy.HasPartner(owner, "Martyrdom"))
			{
				multiplier += 0.5f;
				radius *= 1.5f;
			}
			((CBaseGrenade)projectile).DmgRadius *= radius;
			_nadeMultiplier[projectile.Index] = multiplier;
		});
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info == null || info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		CBaseEntity inflictor = info.Inflictor?.Value;
		if (inflictor == null || inflictor.DesignerName != "hegrenade_projectile")
		{
			return HookResult.Continue;
		}
		if (!_nadeMultiplier.TryGetValue(inflictor.Index, out float multiplier))
		{
			return HookResult.Continue;
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
		if (weapon == null || !weapon.Contains("hegrenade"))
		{
			return HookResult.Continue;
		}
		attacker.GiveNamedItem("weapon_hegrenade");
		attacker.PrintToCenterAlert("手雷击杀！返还一颗手雷");
		return HookResult.Continue;
	}
}
