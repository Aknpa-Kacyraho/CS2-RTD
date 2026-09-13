using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 换位 Swap：按 E 与准星内最近的敌人瞬间互换位置。
/// </summary>
public class Swap : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _cooldowns = new Dictionary<ulong, float>();

	public override string ClassName => "Swap";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged" };

	public Swap(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		if (player == null)
		{
			return 0f;
		}
		return _cooldowns.TryGetValue(player.SteamID, out float value) ? Math.Max(0f, value - Server.CurrentTime) : 0f;
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		_cooldowns.Remove(player.SteamID);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_cooldowns.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (_players.Count == 0 || player == null || !player.IsValid || !_players.Contains(player))
		{
			return;
		}
		if ((pressed & PlayerButtons.Use) == 0)
		{
			return;
		}
		if (GetCooldownRemaining(player) > 0f)
		{
			return;
		}
		CCSPlayerPawn holderPawn = player.PlayerPawn?.Value;
		if (holderPawn == null || !holderPawn.IsValid || holderPawn.LifeState != 0 || holderPawn.AbsOrigin == null)
		{
			return;
		}
		CCSPlayerController target = FindTarget(player, holderPawn);
		if (target == null)
		{
			player.PrintToCenterAlert("换位：准星内没有可交换的敌人");
			return;
		}
		CCSPlayerPawn targetPawn = target.PlayerPawn?.Value;
		if (targetPawn == null || !targetPawn.IsValid || targetPawn.AbsOrigin == null)
		{
			return;
		}
		Vector holderOrigin = new Vector(holderPawn.AbsOrigin.X, holderPawn.AbsOrigin.Y, holderPawn.AbsOrigin.Z);
		Vector targetOrigin = new Vector(targetPawn.AbsOrigin.X, targetPawn.AbsOrigin.Y, targetPawn.AbsOrigin.Z);
		Vector zero = new Vector(0f, 0f, 0f);
		((CBaseEntity)holderPawn).Teleport(targetOrigin, (QAngle)null, zero);
		((CBaseEntity)targetPawn).Teleport(holderOrigin, (QAngle)null, zero);
		_cooldowns[player.SteamID] = Server.CurrentTime + _config.Dices.Swap.Cooldown;
		player.PrintToCenterAlert("换位！");
	}

	private CCSPlayerController? FindTarget(CCSPlayerController holder, CCSPlayerPawn holderPawn)
	{
		float maxDistance = _config.Dices.Swap.MaxDistance;
		float maxAngle = _config.Dices.Swap.MaxAngle;
		float yaw = ((CBaseEntity)holderPawn).AbsRotation.Y;
		float bestAngle = float.MaxValue;
		CCSPlayerController best = null;
		foreach (CCSPlayerController other in Utilities.GetPlayers())
		{
			if (other == null || other.IsHLTV || !other.IsValid || other == holder)
			{
				continue;
			}
			if (((CBaseEntity)other).TeamNum == ((CBaseEntity)holder).TeamNum)
			{
				continue;
			}
			CCSPlayerPawn pawn = other.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || pawn.LifeState != 0 || pawn.AbsOrigin == null)
			{
				continue;
			}
			float dx = pawn.AbsOrigin.X - holderPawn.AbsOrigin.X;
			float dy = pawn.AbsOrigin.Y - holderPawn.AbsOrigin.Y;
			float dz = pawn.AbsOrigin.Z - holderPawn.AbsOrigin.Z;
			if (MathF.Sqrt(dx * dx + dy * dy + dz * dz) > maxDistance)
			{
				continue;
			}
			float targetYaw = MathF.Atan2(dy, dx) * 180f / MathF.PI;
			float diff = Math.Abs(NormalizeAngle(yaw - targetYaw));
			if (diff <= maxAngle && diff < bestAngle)
			{
				bestAngle = diff;
				best = other;
			}
		}
		return best;
	}

	private static float NormalizeAngle(float angle)
	{
		while (angle > 180f)
		{
			angle -= 360f;
		}
		while (angle < -180f)
		{
			angle += 360f;
		}
		return angle;
	}
}
