using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 最后一发 LastStand：弹匣只剩最后一发（<c>Clip1 &lt;= clip_threshold + 1</c>）时，该发伤害 ×damage_multiplier。
///
/// 修复（2026-09-14）：原实现用 <c>EventWeaponFire</c> 在"打空弹匣"后注册加成，但该事件在开火结算**之后**才触发
/// （见 AGENTS 里 Satellite 的同类说明），此时那一发早已打完；且弹匣已空，0.2s 窗口内也不会再有下一发 →
/// ×3 永远不生效。改为每 tick 检测"膛内是否只剩最后一发"并持续注册加成：最后一发打出后弹匣为空（仍满足条件、
/// 无害），换弹后 Clip1 上升即自动撤销。命中/换弹/死亡都会走 Remove/Reset 对称注销，不残留。
/// </summary>
public class LastStand : DiceBlueprint
{
	private readonly HashSet<CCSPlayerController> _buffed = new HashSet<CCSPlayerController>();

	public override string ClassName => "LastStand";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public LastStand(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		DamageBonusManager.Unregister(player, "LastStand");
		_buffed.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			DamageBonusManager.Unregister(player, "LastStand");
		}
		foreach (CCSPlayerController player in _buffed.ToList())
		{
			DamageBonusManager.Unregister(player, "LastStand");
		}
		_players.Clear();
		_buffed.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		foreach (CCSPlayerController stale in _buffed.ToList())
		{
			if (stale == null || !stale.IsValid || !_players.Contains(stale))
			{
				if (stale != null)
				{
					DamageBonusManager.Unregister(stale, "LastStand");
				}
				_buffed.Remove(stale);
			}
		}
		if (_players.Count == 0)
		{
			return;
		}
		int threshold = _config.Dices.LastStand.ClipThreshold;
		float bonus = _config.Dices.LastStand.DamageMultiplier - 1f;
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (player == null || !player.IsValid)
			{
				DamageBonusManager.Unregister(player, "LastStand");
				_buffed.Remove(player);
				continue;
			}
			bool active = false;
			CBasePlayerWeapon weapon = GetActiveWeapon(player);
			if (weapon != null && weapon.IsValid)
			{
				CBasePlayerWeaponVData vData = weapon.VData;
				if (vData != null && vData.MaxClip1 > 1 && weapon.Clip1 <= threshold + 1)
				{
					active = true;
				}
			}
			if (active)
			{
				if (!_buffed.Contains(player))
				{
					DamageBonusManager.Register(player, "LastStand", bonus);
					_buffed.Add(player);
				}
			}
			else if (_buffed.Contains(player))
			{
				DamageBonusManager.Unregister(player, "LastStand");
				_buffed.Remove(player);
			}
		}
	}

	private static CBasePlayerWeapon GetActiveWeapon(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return null;
		}
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)pawn).WeaponServices;
		return weaponServices?.ActiveWeapon?.Value;
	}
}
