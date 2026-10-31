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
/// 光之剑：超新星 Supernova（史诗）。
/// 按 E **充能 0.5s（炮体组装）** 后释放 5s 白蓝电磁炮：从胸口沿准星方向射出 1200 单位，穿墙穿人；
/// 路径上的敌人连续掉血（100 HP/s，按 tick 累加小数，平滑不跳伤）；CD 30s。
/// 开火期间自身减速 50%，可自由转动扫射。直接扣血 → 显式尊重 <see cref="Invulnerability"/>。
///
/// <para>表现：<see cref="RailgunRig"/> —— 用 CBeam 拼出的**电磁轨道炮构造体**（机匣/炮管方框 + 箍环 +
/// 双导轨 + 炮口环 + 线圈 + 握把枪托，参考《蔚蓝档案》爱丽丝的「光之剑：超新星」），充能时逐段组装、
/// 导轨张开，发射时从炮口射出白蓝光束（三层 + 流光环 + 电磁脉冲）。</para>
///
/// <para>零素材依赖：CBeam + 基础游戏粒子/音效（<see cref="ParticlePaths"/> 已预缓存）→ 不订阅工坊也能用。</para>
/// </summary>
public class Supernova : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _chargeEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _fireEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, RailgunRig> _rigs = new Dictionary<ulong, RailgunRig>();
	private readonly List<RailgunRig> _fading = new List<RailgunRig>();
	private readonly Dictionary<ulong, float> _hitNext = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _damageCarry = new Dictionary<ulong, float>();
	private readonly SigilTeardown _fade = new SigilTeardown();

	private const string BuffSource = "Supernova";
	private const float ChestOffset = 50f;
	/// <summary>受害者"躯干中心"相对脚底的高度：射线从胸口射出，必须按躯干判定，否则脚底差 ~64 单位会判空。</summary>
	private const float BodyCenterOffset = 40f;
	/// <summary>命中反馈的最小间隔（防止每 tick 都爆粒子）。</summary>
	private const float HitSparkInterval = 0.15f;

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
		_chargeEnd[player.SteamID] = 0f;
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
			_chargeEnd.Remove(player.SteamID);
			_fireEnd.Remove(player.SteamID);
			_cooldownEnd.Remove(player.SteamID);
			_hitNext.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			EndFire(player);
		}
		foreach (RailgunRig rig in _rigs.Values)
		{
			_fade.Add(new IBeamGroup[] { rig });
		}
		_rigs.Clear();
		foreach (RailgunRig rig in _fading)
		{
			_fade.Add(new IBeamGroup[] { rig });
		}
		_fading.Clear();
		_players.Clear();
		_chargeEnd.Clear();
		_fireEnd.Clear();
		_cooldownEnd.Clear();
		_hitNext.Clear();
		_damageCarry.Clear();
		// 回合结束：不再等分帧队列，直接清干净（下一次开火会重建）。
		_fade.Flush();
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
		float now = Server.CurrentTime;
		if (_players.Count > 0)
		{
			float dt = Server.TickInterval;
			foreach (CCSPlayerController player in _players.ToList())
			{
				try
				{
					if (player == null || !player.IsValid)
					{
						continue;
					}
					if (!_fireEnd.TryGetValue(player.SteamID, out float fireEnd))
					{
						continue;
					}
					// 只在"充能 / 开火中"才动手：未激活的持有者不碰移速字段（否则每 tick 回写会压掉别的系统的移速）。
					if (now >= fireEnd)
					{
						EndFire(player);
						continue;
					}
					TickPlayer(player, dt, now);
				}
				catch
				{
					EndFire(player);
				}
			}
		}
		// 淡出中的炮体：玩家已失效也要收尾（用缓存的帧），完成后转分帧拆除队列。
		for (int i = _fading.Count - 1; i >= 0; i--)
		{
			RailgunRig rig = _fading[i];
			if (!rig.Update(null, null, now))
			{
				_fade.Add(new IBeamGroup[] { rig });
				_fading.RemoveAt(i);
			}
		}
		if (_fade.Any)
		{
			_fade.Tick(now);
		}
	}

	private void TickPlayer(CCSPlayerController player, float dt, float now)
	{
		ulong sid = player.SteamID;
		SupernovaConfig cfg = _config.Dices.Supernova;
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		Vector origin = pawn?.AbsOrigin;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0 || origin == null)
		{
			EndFire(player);
			return;
		}

		Vector forward = ForwardOf(pawn);
		Vector chest = new Vector(origin.X, origin.Y, origin.Z + ChestOffset);
		float chargeEnd = _chargeEnd.TryGetValue(sid, out float ce) ? ce : now;

		// 充能完成 → 开火：炮口爆发（炮体）+ 全服音效 + 震屏
		_rigs.TryGetValue(sid, out RailgunRig rig);
		if (now >= chargeEnd && rig != null && rig.CurrentStage == RailgunRig.Stage.Charge)
		{
			rig.BeginFire(now);
			Effects.SoundAll(cfg.BlastSound, cfg.BlastSoundVolume);
			Effects.Shake(chest, 8f, 45f, 0.3f, 400f);
		}

		// 持续减速（充能 + 开火期间）：与统一移速系统求和，不覆盖其它来源
		pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
		Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);

		// 炮体 / 光束推进
		if (rig != null)
		{
			if (!rig.Update(chest, forward, now))
			{
				_fade.Add(new IBeamGroup[] { rig });
				_rigs.Remove(sid);
			}
		}

		// 伤害（仅开火期；伤害线仍是"胸口 → 胸口 + range×准星"）
		if (now >= chargeEnd)
		{
			float range = cfg.Range;
			Vector end = new Vector(chest.X + forward.X * range, chest.Y + forward.Y * range, chest.Z + forward.Z * range);
			ApplyDamage(player, chest, end, dt, now);
		}
	}

	private void StartFire(CCSPlayerController player)
	{
		float now = Server.CurrentTime;
		ulong sid = player.SteamID;
		SupernovaConfig cfg = _config.Dices.Supernova;
		float charge = Math.Clamp(cfg.ChargeSeconds, 0f, 5f);
		_chargeEnd[sid] = now + charge;
		_fireEnd[sid] = now + charge + MathF.Max(cfg.DurationSeconds, 0.5f);
		_cooldownEnd[sid] = now + MathF.Max(cfg.CooldownSeconds, 1f);
		SpeedBonusManager.Register(player, BuffSource, 0f - Math.Clamp(cfg.SlowPercent, 0f, 1f));

		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		Vector origin = pawn?.AbsOrigin;
		if (pawn != null && pawn.IsValid && origin != null)
		{
			// 炮体总开关在 RailgunRig 里（gun_enabled:false = 只保留光束/流光环）
			Vector chest = new Vector(origin.X, origin.Y, origin.Z + ChestOffset);
			_rigs[sid] = new RailgunRig(cfg, chest, ForwardOf(pawn), now);
		}
		if (origin != null)
		{
			// 召唤瞬间：胸口蓝色能量环（基础游戏粒子，已预缓存）
			Effects.Play(new Vector(origin.X, origin.Y, origin.Z + ChestOffset), ParticlePaths.EnergyCircle, 0.5f);
		}
		player.PrintToCenterAlert("⚔ 光之剑·超新星！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⚔ {player.PlayerName} 召唤了光之剑·超新星！");
	}

	private void EndFire(CCSPlayerController player)
	{
		if (player == null)
		{
			return;
		}
		ulong sid = player.SteamID;
		if (_rigs.TryGetValue(sid, out RailgunRig rig))
		{
			// 进淡出（0.2s 收细），由 OnTick 的 _fading 收尾 → 分帧拆除
			rig.EndFire(Server.CurrentTime);
			_fading.Add(rig);
			_rigs.Remove(sid);
		}
		_chargeEnd.Remove(sid);
		_fireEnd.Remove(sid);
		_hitNext.Remove(sid);
		SpeedBonusManager.Unregister(player, BuffSource);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private static Vector ForwardOf(CCSPlayerPawn pawn)
	{
		QAngle angles = pawn.EyeAngles;
		float pitch = angles.X * MathF.PI / 180f;
		float yaw = angles.Y * MathF.PI / 180f;
		float cp = MathF.Cos(pitch);
		return new Vector(cp * MathF.Cos(yaw), cp * MathF.Sin(yaw), 0f - MathF.Sin(pitch));
	}

	private void ApplyDamage(CCSPlayerController attacker, Vector start, Vector end, float dt, float now)
	{
		Vector hitPoint = null;
		float hitBest = float.MaxValue;
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
			float distance = DistanceToSegment(torso, start, end);
			if (distance > radius)
			{
				continue;
			}
			if (distance < hitBest)
			{
				hitBest = distance;
				hitPoint = torso;
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
		// 命中反馈：电火花落在"最近命中者"的躯干位置（节流，避免每 tick 都爆）。
		if (hitPoint != null && _config.Dices.Supernova.HitSpark)
		{
			ulong attackerId = attacker.SteamID;
			if (!_hitNext.TryGetValue(attackerId, out float sparkAt) || now >= sparkAt)
			{
				_hitNext[attackerId] = now + HitSparkInterval;
				Effects.Play(hitPoint, _config.Dices.Supernova.HitParticle, 0.2f);
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
}
