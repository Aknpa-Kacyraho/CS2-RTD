using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 剑仙 SwordSaint：持刀时，只有面向来袭方向才能挡下子弹（格挡）；背对来袭方向照常受伤。
/// 与 Cutter 组合（剑刃风暴）：格挡时使攻击者减速。
/// </summary>
public class SwordSaint : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, CBasePlayerWeapon> _previousWeapon = new Dictionary<CCSPlayerController, CBasePlayerWeapon>();

	public override string ClassName => "SwordSaint";

	public override List<string> Listeners => new List<string> { "OnTick", "OnPlayerTakeDamagePre" };

	public SwordSaint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_previousWeapon[player] = null;
		if (DiceSynergy.HasPartner(player, "Cutter"))
		{
			DiceSynergy.AnnounceCombo(player, "剑刃风暴", "格挡时使攻击者减速！");
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
		if (player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
		_previousWeapon.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Remove(player);
		}
		_players.Clear();
		_previousWeapon.Clear();
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
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			CBasePlayerWeapon weapon = pawn.WeaponServices?.ActiveWeapon?.Value;
			if (!_previousWeapon.TryGetValue(player, out CBasePlayerWeapon previous) || previous != weapon)
			{
				_previousWeapon[player] = weapon;
				string name = weapon?.DesignerName;
				Color color = (name != null && name.Contains("knife")) ? Color.FromArgb(255, 100, 200, 255) : Color.FromArgb(255, 255, 255, 255);
				((CBaseModelEntity)pawn).Render = color;
				Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender", 0);
			}
		}
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
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (victimPawn == null || !victimPawn.IsValid)
		{
			return HookResult.Continue;
		}
		string weaponName = victimPawn.WeaponServices?.ActiveWeapon?.Value?.DesignerName;
		if (weaponName == null || !weaponName.Contains("knife"))
		{
			return HookResult.Continue;
		}
		if (((uint)info.BitsDamageType & 2u) == 0)
		{
			return HookResult.Continue;
		}
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		if (!IsFacingAttacker(victimPawn, attacker, _config.Dices.SwordSaint.BlockAngleDegrees))
		{
			return HookResult.Continue;
		}
		info.Damage = 0f;
		if (attacker != null && DiceSynergy.HasPartner(victim, "Cutter"))
		{
			CCSPlayerPawn attackerPawn = attacker.PlayerPawn?.Value;
			if (attackerPawn != null && attackerPawn.IsValid)
			{
				SpeedBonusManager.Register(attacker, ClassName, -0.99f, 0.5f);
				attackerPawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
				Utilities.SetStateChanged(attackerPawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				new Timer(0.5f, delegate
				{
					SpeedBonusManager.UnregisterBySteamId(attacker.SteamID, ClassName);
					CCSPlayerPawn after = attacker.PlayerPawn?.Value;
					if (after != null && after.IsValid)
					{
						after.VelocityModifier = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
						Utilities.SetStateChanged(after, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
				}, (TimerFlags?)null);
			}
		}
		victim.PrintToCenterAlert("🗡 剑仙格挡！");
		return HookResult.Changed;
	}

	private static bool IsFacingAttacker(CCSPlayerPawn victim, CCSPlayerController attacker, float angleDegrees)
	{
		if (attacker == null || !attacker.IsValid)
		{
			return false;
		}
		CCSPlayerPawn attackerPawn = attacker.PlayerPawn?.Value;
		if (attackerPawn == null || !attackerPawn.IsValid)
		{
			return false;
		}
		Vector victimPos = ((CBaseEntity)victim).AbsOrigin;
		Vector attackerPos = ((CBaseEntity)attackerPawn).AbsOrigin;
		if (victimPos == null || attackerPos == null)
		{
			return false;
		}
		float dx = attackerPos.X - victimPos.X;
		float dy = attackerPos.Y - victimPos.Y;
		float length = MathF.Sqrt(dx * dx + dy * dy);
		if (length < 1f)
		{
			return true;
		}
		float yaw = victim.EyeAngles.Y * (MathF.PI / 180f);
		float forwardX = MathF.Cos(yaw);
		float forwardY = MathF.Sin(yaw);
		float dot = (forwardX * dx + forwardY * dy) / length;
		float threshold = MathF.Cos(Math.Clamp(angleDegrees, 0f, 180f) * (MathF.PI / 180f));
		return dot >= threshold;
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
