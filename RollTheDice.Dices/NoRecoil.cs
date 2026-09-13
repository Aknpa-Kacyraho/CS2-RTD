using System;
using System.Collections.Generic;
using System.Globalization;
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
/// 铁腕 NoRecoil：静止不动达到 charge_seconds 后进入精准态（无后坐、准心不扩散、伤害加成）；移动立即退出。
/// 与 Skyline 组合（制空权）、与 GunGod 组合（完美枪械）。
/// </summary>
public class NoRecoil : DiceBlueprint
{
	private const float MoveSpeedThreshold = 30f;

	private readonly Dictionary<CCSPlayerController, float> _still = new Dictionary<CCSPlayerController, float>();

	private readonly HashSet<ulong> _active = new HashSet<ulong>();

	private float _lastTick;

	public override string ClassName => "NoRecoil";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public NoRecoil(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_still[player] = 0f;
		if (DiceSynergy.HasPartner(player, "GunGod"))
		{
			DiceSynergy.AnnounceCombo(player, "完美枪械", "枪神减伤更高，任意武器零扩散！");
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
		Deactivate(player);
		_still.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Deactivate(player);
		}
		_still.Clear();
		_active.Clear();
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
		NoRecoilConfig cfg = _config.Dices.NoRecoil;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			Vector velocity = ((CBaseEntity)pawn).AbsVelocity;
			float horizontal = (velocity == null) ? 0f : MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
			bool onGround = (((CBaseEntity)pawn).Flags & 1u) != 0;
			float still = (_still.TryGetValue(player, out float s) ? s : 0f);
			if (onGround && horizontal <= MoveSpeedThreshold)
			{
				still += dt;
			}
			else
			{
				still = 0f;
			}
			_still[player] = still;
			bool active = still >= cfg.ChargeSeconds;
			bool wasActive = _active.Contains(player.SteamID);
			if (active && !wasActive)
			{
				_active.Add(player.SteamID);
				DamageBonusManager.Register(player, ClassName, cfg.DamageBonus);
				player.ReplicateConVar("weapon_accuracy_nospread", "1");
			}
			else if (!active && wasActive)
			{
				Deactivate(player);
			}
			if (active)
			{
				ApplyNoRecoil(player);
			}
		}
	}

	private void Deactivate(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		if (_active.Remove(player.SteamID))
		{
			DamageBonusManager.Unregister(player, ClassName);
			player.ReplicateConVar("weapon_accuracy_nospread", "0");
		}
	}

	public static void ApplyNoRecoil(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value.AimPunchServices == null)
		{
			return;
		}
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)player.PlayerPawn.Value).WeaponServices;
		CBasePlayerWeapon weapon = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
		if (weapon == null || !weapon.IsValid)
		{
			return;
		}
		CCSWeaponBase weaponBase = weapon.As<CCSWeaponBase>();
		if (weaponBase == null)
		{
			return;
		}
		string name = weapon.DesignerName?.ToLower(CultureInfo.CurrentCulture);
		if (DiceSynergy.HasPartner(player, "GunGod") || (name != null && !name.Contains("mag7") && !name.Contains("nova") && !name.Contains("sawedoff") && !name.Contains("xm1014")))
		{
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.X = 0f;
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.Y = 0f;
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.Z = 0f;
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.X = 0f;
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.Y = 0f;
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.Z = 0f;
			player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.X = 0f;
			player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Y = 0f;
			player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Z = 0f;
			player.PlayerPawn.Value.AimPunchServices.PredictableBaseTick = -1;
			player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseTick = -1;
			weaponBase.AccuracyPenalty = 0f;
			weaponBase.FlRecoilIndex = 0f;
		}
	}
}
