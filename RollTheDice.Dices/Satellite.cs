using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 卫星 Satellite：极低重力 + 空中射击精准；滞空时移速提升（滑翔）。
/// 与 Drone 组合（天网）。
/// </summary>
public class Satellite : DiceBlueprint
{
	private readonly HashSet<ulong> _airNoSpread = new HashSet<ulong>();

	private readonly HashSet<ulong> _airSpeed = new HashSet<ulong>();

	public override string ClassName => "Satellite";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public override List<string> Events => new List<string> { "EventWeaponFire" };

	public Satellite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		if (DiceSynergy.HasPartner(player, "Drone"))
		{
			DiceSynergy.AnnounceCombo(player, "天网", "无人机伤害提升，卫星浮空！");
		}
		float gravity = _config.Dices.Satellite.Gravity;
		((CBaseEntity)player.PlayerPawn.Value).ActualGravityScale = gravity;
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
		if (player != null && player.IsValid)
		{
			_airNoSpread.Remove(player.SteamID);
			_airSpeed.Remove(player.SteamID);
			RestoreNoSpread(player);
			SpeedBonusManager.Unregister(player, ClassName);
			CCSPlayerPawn pawn = player.PlayerPawn?.Value;
			if (pawn != null && pawn.IsValid)
			{
				((CBaseEntity)pawn).ActualGravityScale = 1f;
				pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Remove(player);
		}
		_airNoSpread.Clear();
		_airSpeed.Clear();
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
		float airBonus = _config.Dices.Satellite.AirSpeedBonus;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			float gravity = _config.Dices.Satellite.Gravity * (DiceSynergy.HasPartner(player, "Drone") ? 0.3f : 1f);
			((CBaseEntity)pawn).ActualGravityScale = gravity;
			bool airborne = (((CBaseEntity)pawn).Flags & 1u) == 0;
			bool applied = _airNoSpread.Contains(player.SteamID);
			if (airborne && !applied)
			{
				player.ReplicateConVar("weapon_accuracy_nospread", "1");
				_airNoSpread.Add(player.SteamID);
			}
			else if (!airborne && applied)
			{
				_airNoSpread.Remove(player.SteamID);
				RestoreNoSpread(player);
			}
			if (airborne)
			{
				CBasePlayerWeapon weapon = pawn.WeaponServices?.ActiveWeapon?.Value;
				if (weapon != null && weapon.IsValid)
				{
					CCSWeaponBase weaponBase = weapon.As<CCSWeaponBase>();
					if (weaponBase != null)
					{
						weaponBase.AccuracyPenalty = 0f;
						weaponBase.FlRecoilIndex = 0f;
					}
				}
				if (!_airSpeed.Contains(player.SteamID))
				{
					_airSpeed.Add(player.SteamID);
					SpeedBonusManager.Register(player, ClassName, airBonus);
				}
				pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
			else if (_airSpeed.Remove(player.SteamID))
			{
				SpeedBonusManager.Unregister(player, ClassName);
				pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		CCSPlayerController userid = @event.Userid;
		if (userid == null || !_players.Contains(userid))
		{
			return HookResult.Continue;
		}
		CBasePlayerWeapon weapon = userid.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value;
		if (weapon != null && weapon.IsValid)
		{
			CCSWeaponBase weaponBase = weapon.As<CCSWeaponBase>();
			if (weaponBase != null)
			{
				weaponBase.AccuracyPenalty = 0f;
				weaponBase.FlRecoilIndex = 0f;
			}
		}
		return HookResult.Continue;
	}

	private static void RestoreNoSpread(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		if (RollTheDice.Instance?.HasDiceActive(player, "NoRecoil") == true)
		{
			return;
		}
		player.ReplicateConVar("weapon_accuracy_nospread", "0");
	}
}
