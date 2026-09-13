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
/// 狙击精英 SniperElite：使用狙击枪时，开镜停留越久伤害越高（最多 +200%），收镜/移动重置。
/// 与 DeagleKing 组合（精准猎杀）：开镜蓄力速度 ×1.5。
/// </summary>
public class SniperElite : DiceBlueprint
{
	private static readonly HashSet<string> SniperWeapons = new HashSet<string> { "weapon_awp", "weapon_ssg08", "weapon_scar20", "weapon_g3sg1" };

	private readonly Dictionary<CCSPlayerController, float> _charge = new Dictionary<CCSPlayerController, float>();

	private float _lastTick;

	public override string ClassName => "SniperElite";

	public override List<string> Listeners => new List<string> { "OnTick", "OnPlayerTakeDamagePre" };

	public SniperElite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_charge[player] = 0f;
		if (DiceSynergy.HasPartner(player, "DeagleKing"))
		{
			DiceSynergy.AnnounceCombo(player, "精准猎杀", "开镜蓄力速度 ×1.5！");
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
		_charge.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_charge.Clear();
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
		float dt = (_lastTick <= 0f) ? 0f : Math.Min(now - _lastTick, 0.25f);
		_lastTick = now;
		if (dt <= 0f)
		{
			return;
		}
		SniperEliteConfig cfg = _config.Dices.SniperElite;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid)
			{
				continue;
			}
			CBasePlayerWeapon weapon = pawn.WeaponServices?.ActiveWeapon?.Value;
			bool scoped = false;
			if (weapon != null && weapon.IsValid)
			{
				string name = weapon.DesignerName;
				if (name != null && SniperWeapons.Contains(name) && weapon is CCSWeaponBaseGun gun && gun.ZoomLevel > 0)
				{
					scoped = true;
				}
			}
			if (!scoped)
			{
				_charge[player] = 0f;
				continue;
			}
			float rate = DiceSynergy.HasPartner(player, "DeagleKing") ? 1.5f : 1f;
			float current = (_charge.TryGetValue(player, out float c) ? c : 0f);
			_charge[player] = Math.Min(current + dt * rate, cfg.ChargeSeconds);
		}
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
		if (weapon == null || !weapon.IsValid)
		{
			return HookResult.Continue;
		}
		string name = weapon.DesignerName;
		if (name == null || !SniperWeapons.Contains(name))
		{
			return HookResult.Continue;
		}
		SniperEliteConfig cfg = _config.Dices.SniperElite;
		float charge = (_charge.TryGetValue(attacker, out float c) ? c : 0f);
		float ratio = (cfg.ChargeSeconds <= 0f) ? 1f : Math.Min(charge / cfg.ChargeSeconds, 1f);
		info.Damage *= 1f + ratio * cfg.MaxBonus;
		return HookResult.Changed;
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
