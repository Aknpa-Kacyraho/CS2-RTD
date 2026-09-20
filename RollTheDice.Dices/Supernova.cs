using System;
using System.Collections.Generic;
using System.Drawing;
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
/// 光之剑：超新星 Supernova（史诗）。
/// 按 E 释放 5s 金色激光：从胸口沿准星方向射出 1200 单位，穿墙穿人；
/// 路径上的敌人连续掉血（100 HP/s，按 tick 累加小数，平滑不跳伤）；CD 30s。
/// 开火期间自身减速 50%，可自由转动扫射。直接扣血 → 显式尊重 <see cref="Invulnerability"/>。
/// </summary>
public class Supernova : DiceBlueprint
{
	private sealed class BeamPair
	{
		public CBeam Outer;
		public CBeam Inner;
	}

	private readonly Dictionary<ulong, float> _fireEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, BeamPair> _beams = new Dictionary<ulong, BeamPair>();
	private readonly Dictionary<ulong, float> _muzzleNext = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _damageCarry = new Dictionary<ulong, float>();

	private const string BuffSource = "Supernova";
	private const float ChestOffset = 50f;
	/// <summary>受害者"躯干中心"相对脚底的高度：射线从胸口射出，必须按躯干判定，否则脚底差 ~64 单位会判空。</summary>
	private const float BodyCenterOffset = 40f;
	private const float MuzzleInterval = 0.1f;

	public override string ClassName => "Supernova";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public Supernova(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_fireEnd[player.SteamID] = 0f;
		_cooldownEnd[player.SteamID] = 0f;
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { { "playerName", player.PlayerName } });
		player.PrintToCenterAlert("⚔ 按E释放光之剑·超新星！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player != null)
		{
			EndFire(player);
			_fireEnd.Remove(player.SteamID);
			_cooldownEnd.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			EndFire(player);
		}
		_players.Clear();
		_fireEnd.Clear();
		_cooldownEnd.Clear();
		_muzzleNext.Clear();
		_damageCarry.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return 0f;
		}
		float now = Server.CurrentTime;
		if (_fireEnd.TryGetValue(player.SteamID, out float fireEnd) && now < fireEnd)
		{
			return 0f;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cooldown) && now < cooldown)
		{
			return cooldown - now;
		}
		return 0f;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (player == null || !player.IsValid || (pressed & PlayerButtons.Use) == 0)
		{
			return;
		}
		if (!_players.Contains(player))
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		if (_fireEnd.TryGetValue(player.SteamID, out float fireEnd) && now < fireEnd)
		{
			return;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cooldown) && now < cooldown)
		{
			return;
		}
		StartFire(player);
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		float dt = Server.TickInterval;
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				if (player == null || !player.IsValid)
				{
					continue;
				}
				// 未在开火 → 什么都不做（避免每 tick 空跑 EndFire / 写移速）
				if (!_fireEnd.TryGetValue(player.SteamID, out float fireEnd))
				{
					continue;
				}
				if (now >= fireEnd)
				{
					EndFire(player);
					continue;
				}
				CCSPlayerPawn pawn = player.PlayerPawn?.Value;
				Vector origin = pawn?.AbsOrigin;
				if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0 || origin == null)
				{
					EndFire(player);
					continue;
				}

				// 持续减速：与统一移速系统求和，不覆盖其它来源
				pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);

				QAngle angles = pawn.EyeAngles;
				float pitch = angles.X * MathF.PI / 180f;
				float yaw = angles.Y * MathF.PI / 180f;
				float cp = MathF.Cos(pitch);
				float sp = MathF.Sin(pitch);
				float fx = cp * MathF.Cos(yaw);
				float fy = cp * MathF.Sin(yaw);
				float fz = 0f - sp;
				float range = _config.Dices.Supernova.Range;
				Vector start = new Vector(origin.X, origin.Y, origin.Z + ChestOffset);
				Vector end = new Vector(start.X + fx * range, start.Y + fy * range, start.Z + fz * range);
				UpdateBeams(player.SteamID, start, end);

				if (!_muzzleNext.TryGetValue(player.SteamID, out float muzzleAt) || now >= muzzleAt)
				{
					_muzzleNext[player.SteamID] = now + MuzzleInterval;
					Effects.Play(start, ParticlePaths.GoldHaloFlare, 0.3f);
				}

				ApplyDamage(player, start, end, dt);
			}
			catch
			{
				EndFire(player);
			}
		}
	}

	private void StartFire(CCSPlayerController player)
	{
		float now = Server.CurrentTime;
		ulong sid = player.SteamID;
		_fireEnd[sid] = now + _config.Dices.Supernova.DurationSeconds;
		_cooldownEnd[sid] = now + _config.Dices.Supernova.CooldownSeconds;
		_muzzleNext[sid] = 0f;
		SpeedBonusManager.Register(player, BuffSource, 0f - _config.Dices.Supernova.SlowPercent);
		player.PrintToCenterAlert("⚔ 光之剑·超新星！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⚔ {player.PlayerName} 拔出了光之剑·超新星！");
	}

	private void EndFire(CCSPlayerController player)
	{
		if (player == null)
		{
			return;
		}
		ulong sid = player.SteamID;
		RemoveBeams(sid);
		_muzzleNext.Remove(sid);
		_fireEnd.Remove(sid);
		SpeedBonusManager.Unregister(player, BuffSource);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private void ApplyDamage(CCSPlayerController attacker, Vector start, Vector end, float dt)
	{
		float dps = _config.Dices.Supernova.DamagePerSecond;
		if (DiceSynergy.HasPartner(attacker, "DivinePunishment"))
		{
			dps *= 1.5f;
		}
		float radius = _config.Dices.Supernova.HitRadius;
		if (DiceSynergy.HasPartner(attacker, "LaserCage"))
		{
			radius += 40f;
		}
		foreach (CCSPlayerController victim in Utilities.GetPlayers())
		{
			if (victim == null || !victim.IsValid || victim.IsHLTV)
			{
				continue;
			}
			if (((CBaseEntity)victim).TeamNum == ((CBaseEntity)attacker).TeamNum)
			{
				continue;
			}
			CCSPlayerPawn vp = victim.PlayerPawn?.Value;
			Vector pos = vp?.AbsOrigin;
			if (vp == null || !vp.IsValid || ((CBaseEntity)vp).LifeState != 0 || pos == null)
			{
				continue;
			}
			// 用躯干中心（脚底 + 40）判定，和从胸口射出的光束处在同一高度。
			Vector torso = new Vector(pos.X, pos.Y, pos.Z + BodyCenterOffset);
			if (DistanceToSegment(torso, start, end) > radius)
			{
				continue;
			}
			if (Invulnerability.IsInvulnerable(victim))
			{
				continue;
			}
			ulong vid = victim.SteamID;
			_damageCarry.TryGetValue(vid, out float carry);
			carry += dps * dt;
			int whole = (int)carry;
			_damageCarry[vid] = carry - whole;
			if (whole <= 0)
			{
				continue;
			}
			((CBaseEntity)vp).Health -= whole;
			Utilities.SetStateChanged((CBaseEntity)vp, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)vp).Health > 0)
			{
				continue;
			}
			try
			{
				((CBasePlayerPawn)vp).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)vp).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)vp, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}

	private static float DistanceToSegment(Vector point, Vector a, Vector b)
	{
		float abx = b.X - a.X;
		float aby = b.Y - a.Y;
		float abz = b.Z - a.Z;
		float apx = point.X - a.X;
		float apy = point.Y - a.Y;
		float apz = point.Z - a.Z;
		float ab2 = abx * abx + aby * aby + abz * abz;
		if (ab2 <= 0.0001f)
		{
			return MathF.Sqrt(apx * apx + apy * apy + apz * apz);
		}
		float t = (apx * abx + apy * aby + apz * abz) / ab2;
		// 只判定线段正投影范围内的目标，避免把身后 / 超出射程的人也算命中
		if (t < 0f || t > 1f)
		{
			return float.PositiveInfinity;
		}
		float dx = apx - abx * t;
		float dy = apy - aby * t;
		float dz = apz - abz * t;
		return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
	}

	private void UpdateBeams(ulong sid, Vector start, Vector end)
	{
		EnsureBeams(sid);
		if (!_beams.TryGetValue(sid, out BeamPair pair))
		{
			return;
		}
		MoveBeam(pair.Outer, start, end);
		MoveBeam(pair.Inner, start, end);
	}

	private void EnsureBeams(ulong sid)
	{
		if (_beams.TryGetValue(sid, out BeamPair existing) && IsBeamValid(existing.Outer) && IsBeamValid(existing.Inner))
		{
			return;
		}
		RemoveBeams(sid);
		SupernovaConfig cfg = _config.Dices.Supernova;
		float outerWidth = MathF.Max(cfg.BeamWidthOuter, 1f);
		float innerWidth = Math.Clamp(cfg.BeamWidthInner, 0.5f, outerWidth);
		BeamPair pair = new BeamPair();
		pair.Outer = CreateBeam(Color.FromArgb(255, 255, 215, 0), outerWidth);
		pair.Inner = CreateBeam(Color.FromArgb(255, 255, 255, 255), innerWidth);
		_beams[sid] = pair;
	}

	private static CBeam CreateBeam(Color color, float width)
	{
		return BeamFx.Create(color, width)!;
	}

	private static void MoveBeam(CBeam beam, Vector start, Vector end)
	{
		BeamFx.Move(beam, start, end);
	}

	private static bool IsBeamValid(CBeam beam)
	{
		return BeamFx.IsAlive(beam);
	}

	private void RemoveBeams(ulong sid)
	{
		if (!_beams.TryGetValue(sid, out BeamPair pair))
		{
			return;
		}
		RemoveBeam(pair.Outer);
		RemoveBeam(pair.Inner);
		_beams.Remove(sid);
	}

	private static void RemoveBeam(CBeam beam)
	{
		BeamFx.Kill(beam);
	}
}
