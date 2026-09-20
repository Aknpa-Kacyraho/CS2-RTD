#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 每个 dice 的粒子/特效映射表已迁出到 <c>configs/plugins/RollTheDice/dicefx.json</c>（见 <see cref="DiceFxTable"/>）；
/// 本类只做<b>引擎</b>：事件分发、挂点几何（按玩家朝向实时计算）、粒子实体生命周期。
///
/// 设计原则：
/// <list type="bullet">
/// <item><b>不挂常驻光环</b>。持续附着 / 环绕都用不挂 Parent 的粒子实体，每 tick 由 <see cref="OnTick"/> 重新定位。</item>
/// <item>粒子路径必须在 CS2 VPK 里真实存在（见 <see cref="ParticlePaths"/>），写错会静默失败。</item>
/// </list>
/// </summary>
public static class DiceEffects
{
	private static readonly HashSet<string> Empty = new HashSet<string>();

	private static readonly Dictionary<ulong, HashSet<string>> Active = new Dictionary<ulong, HashSet<string>>();

	private static readonly Dictionary<ulong, float> LastProc = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, float> LastFire = new Dictionary<ulong, float>();

	private static readonly Dictionary<(ulong SteamId, string ClassName), float> TrailNext = new Dictionary<(ulong, string), float>();

	// 持续附着 / 环绕（不挂 Parent，每 tick MoveTo 跟随）
	private static readonly Dictionary<(ulong SteamId, string ClassName), CParticleSystem> Holds = new Dictionary<(ulong, string), CParticleSystem>();
	private static readonly Dictionary<(ulong SteamId, string ClassName), CParticleSystem> Orbits = new Dictionary<(ulong, string), CParticleSystem>();

	// 光翼（CBeam 自造，每 tick 按朝向重算，见 WingFx）。ui_status_level_wings 是 world-space 复合粒子，
	// 每 tick MoveTo 也带不走，所以 Wings 模式一律走这里。
	private static readonly Dictionary<(ulong SteamId, string ClassName), WingFx> Winged = new Dictionary<(ulong, string), WingFx>();

	public static bool Enabled { get; set; } = true;

	/// <summary>只关掉"持续类"特效（脚底痕迹 / 持续附着 / 环绕），保留抽到/击杀等触发特效。</summary>
	public static bool TrailsEnabled { get; set; } = true;

	private const float ProcInterval = 0.25f;

	private const float FireInterval = 0.12f;

	private const float BurstLife = 2f;

	private const float EventLife = 1.6f;

	private const float DeathLife = 2.2f;

	private const float FireLife = 0.4f;

	private const float BurstZOffset = 40f;

	private const float EventZOffset = 32f;

	private const float DeathZOffset = 8f;

	// ── 声明式 CBeam 光束（dicefx.json 的 beams）──
	private sealed class ActiveRing
	{
		public required MagicCircle Circle;
		public required Vector Center;
		public float Start;
		public float Life;
		public float Spin;
		public float Radius;
		public float Outer;
		public float Grow;
	}

	private static readonly List<ActiveRing> ActiveRings = new List<ActiveRing>();

	private static Color BeamColor(int[]? rgb)
	{
		if (rgb == null || rgb.Length < 3)
		{
			return Color.FromArgb(255, 255, 215, 0);
		}
		return Color.FromArgb(255, Math.Clamp(rgb[0], 0, 255), Math.Clamp(rgb[1], 0, 255), Math.Clamp(rgb[2], 0, 255));
	}

	/// <summary>按规格播放一组光束（ring = 缓存的魔法阵句柄，由 OnTick 驱动旋转/缩放；pillar = 一次性光柱）。</summary>
	private static void PlayBeams(List<FxBeamSpec>? specs, Vector? center)
	{
		if (specs == null || center == null)
		{
			return;
		}
		foreach (FxBeamSpec spec in specs)
		{
			if (spec == null)
			{
				continue;
			}
			if (string.Equals(spec.Shape, "pillar", StringComparison.OrdinalIgnoreCase))
			{
				BeamFx.Pillar(center, spec.Height, BeamColor(spec.Color), spec.Width, spec.Life);
				continue;
			}
			MagicCircle circle = BeamFx.Ring(center, spec.Radius, spec.Outer, BeamColor(spec.Color), spec.Width, spec.Segments, spec.Spokes, spec.Z);
			ActiveRings.Add(new ActiveRing
			{
				Circle = circle,
				Center = center,
				Start = Server.CurrentTime,
				Life = MathF.Max(spec.Life, 0.1f),
				Spin = spec.Spin,
				Radius = spec.Radius,
				Outer = spec.Outer,
				Grow = spec.Grow
			});
		}
	}

	private static void UpdateBeams()
	{
		if (ActiveRings.Count == 0)
		{
			return;
		}
		try
		{
			float now = Server.CurrentTime;
			for (int i = ActiveRings.Count - 1; i >= 0; i--)
			{
				ActiveRing r = ActiveRings[i];
				float t = (now - r.Start) / r.Life;
				if (t >= 1f)
				{
					r.Circle.Remove();
					ActiveRings.RemoveAt(i);
					continue;
				}
				float rotation = t * r.Spin * MathF.PI * 2f;
				float radius = r.Radius + r.Grow * t;
				float outer = r.Outer > 0f ? r.Outer + r.Grow * t : 0f;
				r.Circle.SetRadius(radius, outer);
				r.Circle.Update(r.Center, rotation);
			}
		}
		catch
		{
		}
	}

	private static void ClearBeams()
	{
		foreach (ActiveRing r in ActiveRings)
		{
			r.Circle.Remove();
		}
		ActiveRings.Clear();
	}

	public static void OnDiceAdded(CCSPlayerController? player, string? className)
	{
		if (player == null || !player.IsValid || string.IsNullOrEmpty(className))
		{
			return;
		}
		if (!Enabled)
		{
			return;
		}
		if (!DiceFxTable.TryGet(className, out FxProfile profile))
		{
			return;
		}
		try
		{
			ulong id = player.SteamID;
			if (!Active.TryGetValue(id, out HashSet<string> dice))
			{
				dice = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				Active[id] = dice;
			}
			dice.Add(className);
			(ulong, string) key = (id, className);
			TrailNext.Remove(key);
			RemovePersistentByClass(id, className);
			if (!string.IsNullOrEmpty(profile.Trigger.Burst))
			{
				PlayAtPlayer(player, profile.Trigger.Burst!, BurstLife, BurstZOffset);
			}
			PlayBeams(profile.Beams?.Trigger, PlayerOrigin(player, BurstZOffset));
		}
		catch
		{
		}
	}

	public static void OnDiceRemoved(CCSPlayerController? player, string? className)
	{
		if (player == null || string.IsNullOrEmpty(className))
		{
			return;
		}
		try
		{
			ulong id = player.SteamID;
			if (Active.TryGetValue(id, out HashSet<string> dice))
			{
				dice.Remove(className);
				if (dice.Count == 0)
				{
					Active.Remove(id);
					LastProc.Remove(id);
					LastFire.Remove(id);
				}
			}
			(ulong, string) key = (id, className);
			TrailNext.Remove(key);
			RemovePersistentByClass(id, className);
			if (Enabled && player.IsValid && DiceFxTable.TryGet(className, out FxProfile profile))
			{
				if (!string.IsNullOrEmpty(profile.Trigger.Remove))
				{
					PlayAtPlayer(player, profile.Trigger.Remove!, EventLife, BurstZOffset);
				}
				PlayBeams(profile.Beams?.Remove, PlayerOrigin(player, BurstZOffset));
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// 持续类特效的中央驱动器：主插件每 tick 调用一次。
	/// ① 脚底痕迹（Trail）按间隔留下小粒子；② 持续附着（Hold）与环绕（Orbit）用不挂 Parent 的实体每 tick 跟随。
	/// 只对存活真人工作。
	/// </summary>
	public static void OnTick()
	{
		// 声明式光束（环）的旋转/缩放先更新，不受 TrailsEnabled 影响。
		UpdateBeams();
		// 本方法只驱动"持续类"特效（痕迹 / 附着 / 环绕）。
		if (!Enabled || !TrailsEnabled)
		{
			// 关掉持续特效时收掉已存在的附着/环绕，避免留在原地。
			ClearPersistentOnly();
			return;
		}
		if (Active.Count == 0)
		{
			return;
		}
		try
		{
			float now = Server.CurrentTime;
			foreach (CCSPlayerController player in Utilities.GetPlayers())
			{
				if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
				{
					continue;
				}
				if (!Active.TryGetValue(player.SteamID, out HashSet<string> dice) || dice.Count == 0)
				{
					continue;
				}
				CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
				if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
				{
					continue;
				}
				Vector? origin = pawn.AbsOrigin;
				if (origin == null)
				{
					continue;
				}
				// 按朝向求前 / 右水平轴（背后与双翼都基于朝向，永不转向准星前方）
				float yaw = pawn.EyeAngles.Y * MathF.PI / 180f;
				float fwdX = MathF.Cos(yaw);
				float fwdY = MathF.Sin(yaw);
				foreach (string className in dice)
				{
					if (!DiceFxTable.TryGet(className, out FxProfile profile))
					{
						continue;
					}
					(ulong, string) key = (player.SteamID, className);
					if (TrailsEnabled && !string.IsNullOrEmpty(profile.Trail.Particle))
					{
						if (!TrailNext.TryGetValue(key, out float next) || now >= next)
						{
							TrailNext[key] = now + profile.Trail.Interval;
							Effects.Play(new Vector(origin.X, origin.Y, origin.Z + profile.Trail.Z), profile.Trail.Particle!, profile.Trail.Interval + 0.5f);
						}
					}
					bool wingUpdated = false;
					if (TrailsEnabled && profile.Attach.Mode == FxMode.Wings)
					{
						UpdateWing(key, profile, origin, yaw, now);
						wingUpdated = true;
					}
					else if (TrailsEnabled && !string.IsNullOrEmpty(profile.Attach.Particle))
					{
						Vector holdPos = new Vector(origin.X, origin.Y, origin.Z + profile.Attach.Z);
						if (profile.Attach.Mode == FxMode.Behind)
						{
							holdPos = new Vector(origin.X - fwdX * profile.Geom.Behind, origin.Y - fwdY * profile.Geom.Behind, origin.Z + profile.Attach.Z);
						}
						HoldPersistent(Holds, key, profile.Attach.Particle!, holdPos);
					}
					if (!wingUpdated && TrailsEnabled && profile.Orbit.Mode == FxMode.Wings)
					{
						UpdateWing(key, profile, origin, yaw, now);
					}
					else if (TrailsEnabled && !string.IsNullOrEmpty(profile.Orbit.Particle))
					{
						switch (profile.Orbit.Mode)
						{
						case FxMode.Behind:
						{
							float bob = MathF.Sin(now * profile.Orbit.Speed) * 3f;
							Vector behind = new Vector(
								origin.X - fwdX * profile.Geom.Behind,
								origin.Y - fwdY * profile.Geom.Behind,
								origin.Z + profile.Orbit.Z + bob);
							HoldPersistent(Orbits, key, profile.Orbit.Particle!, behind);
							break;
						}
						default:
						{
							float phase = now * profile.Orbit.Speed + (float)(player.SteamID % 6283) / 1000f;
							Vector orbitPos = new Vector(
								origin.X + MathF.Cos(phase) * profile.Orbit.Radius,
								origin.Y + MathF.Sin(phase) * profile.Orbit.Radius,
								origin.Z + profile.Orbit.Z);
							HoldPersistent(Orbits, key, profile.Orbit.Particle!, orbitPos);
							break;
						}
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>枪口 / 弹道：由主插件的 EventWeaponFire 调用（按 <see cref="FireInterval"/> 节流）。</summary>
	public static void OnWeaponFire(CCSPlayerController? player)
	{
		if (!Enabled || player == null || !player.IsValid)
		{
			return;
		}
		try
		{
			HashSet<string> dice = DiceOf(player);
			if (dice.Count == 0)
			{
				return;
			}
			// 刀 / 手雷 / C4 没有枪口，跳过。
			string? weapon = ActiveWeaponName(player);
			if (string.IsNullOrEmpty(weapon)
				|| weapon!.Contains("knife", StringComparison.OrdinalIgnoreCase)
				|| weapon.Contains("grenade", StringComparison.OrdinalIgnoreCase)
				|| weapon.Contains("c4", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			float now = Server.CurrentTime;
			if (LastFire.TryGetValue(player.SteamID, out float last) && now - last < FireInterval)
			{
				return;
			}
			LastFire[player.SteamID] = now;
			foreach (string className in dice)
			{
				if (DiceFxTable.TryGet(className, out FxProfile profile) && !string.IsNullOrEmpty(profile.Events.Fire))
				{
					Effects.PlayAtCrosshair(player, profile.Events.Fire!, 50f, FireLife);
				}
			}
		}
		catch
		{
		}
	}

	public static void OnPlayerKill(CCSPlayerController? attacker, CCSPlayerController? victim, bool headshot = false)
	{
		if (!Enabled || attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
		{
			return;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return;
		}
		Vector? victimPos = victim.PlayerPawn?.Value?.AbsOrigin;
		Vector? attackerPos = attacker.PlayerPawn?.Value?.AbsOrigin;
		foreach (string className in DiceOf(attacker))
		{
			if (!DiceFxTable.TryGet(className, out FxProfile profile))
			{
				continue;
			}
			string? onVictim = (headshot ? (profile.Events.KillHeadshot ?? profile.Events.Kill) : profile.Events.Kill);
			if (!string.IsNullOrEmpty(onVictim) && victimPos != null)
			{
				Effects.Play(new Vector(victimPos.X, victimPos.Y, victimPos.Z + DeathZOffset), onVictim!, EventLife);
			}
			if (!string.IsNullOrEmpty(profile.Events.KillSelf) && attackerPos != null)
			{
				Effects.Play(new Vector(attackerPos.X, attackerPos.Y, attackerPos.Z + BurstZOffset), profile.Events.KillSelf!, EventLife);
			}
			if (victimPos != null)
			{
				PlayBeams(profile.Beams?.Kill, new Vector(victimPos.X, victimPos.Y, victimPos.Z + DeathZOffset));
			}
			if (attackerPos != null)
			{
				PlayBeams(profile.Beams?.KillSelf, new Vector(attackerPos.X, attackerPos.Y, attackerPos.Z + BurstZOffset));
			}
		}
	}

	public static void OnPlayerDeath(CCSPlayerController? victim)
	{
		if (!Enabled || victim == null || !victim.IsValid)
		{
			return;
		}
		// 死亡时收掉持续附着/环绕，避免残留在死亡点。
		RemovePlayerPersistent(victim.SteamID);
		Vector? pos = victim.PlayerPawn?.Value?.AbsOrigin;
		if (pos == null)
		{
			return;
		}
		foreach (string className in DiceOf(victim))
		{
			if (!DiceFxTable.TryGet(className, out FxProfile profile))
			{
				continue;
			}
			if (!string.IsNullOrEmpty(profile.Events.Death))
			{
				Effects.Play(new Vector(pos.X, pos.Y, pos.Z + DeathZOffset), profile.Events.Death!, DeathLife);
			}
			PlayBeams(profile.Beams?.Death, new Vector(pos.X, pos.Y, pos.Z + DeathZOffset));
		}
	}

	public static void OnPlayerDamaged(CCSPlayerController? victim, CCSPlayerController? attacker, bool headshot = false)
	{
		if (!Enabled || victim == null || !victim.IsValid)
		{
			return;
		}
		try
		{
			float now = Server.CurrentTime;
			Vector? pos = victim.PlayerPawn?.Value?.AbsOrigin;
			if (pos == null)
			{
				return;
			}
			List<(string Path, Vector Pos)> pending = new List<(string, Vector)>();
			HashSet<string> victimDice = DiceOf(victim);
			if (victimDice.Count > 0 && CanProc(victim.SteamID, now))
			{
				foreach (string className in victimDice)
				{
					if (!DiceFxTable.TryGet(className, out FxProfile profile))
					{
						continue;
					}
					if (!string.IsNullOrEmpty(profile.Events.Hurt))
					{
						pending.Add((profile.Events.Hurt!, new Vector(pos.X, pos.Y, pos.Z + EventZOffset)));
					}
					PlayBeams(profile.Beams?.Hurt, new Vector(pos.X, pos.Y, pos.Z + EventZOffset));
				}
			}
			bool enemy = attacker != null && attacker.IsValid && !Equals(attacker, victim)
				&& ((CBaseEntity)attacker).TeamNum != ((CBaseEntity)victim).TeamNum;
			if (enemy)
			{
				HashSet<string> attackerDice = DiceOf(attacker!);
				if (attackerDice.Count > 0 && CanProc(attacker!.SteamID, now))
				{
					foreach (string className in attackerDice)
					{
						if (!DiceFxTable.TryGet(className, out FxProfile profile))
						{
							continue;
						}
						string? onHit = headshot ? (profile.Events.HitHeadshot ?? profile.Events.Hit) : profile.Events.Hit;
						if (!string.IsNullOrEmpty(onHit))
						{
							pending.Add((onHit!, new Vector(pos.X, pos.Y, pos.Z + EventZOffset)));
						}
						PlayBeams(profile.Beams?.Hit, new Vector(pos.X, pos.Y, pos.Z + EventZOffset));
					}
				}
			}
			if (pending.Count > 0)
			{
				Server.NextFrame(delegate
				{
					foreach ((string path, Vector p) in pending)
					{
						Effects.Play(p, path, EventLife);
					}
				});
			}
		}
		catch
		{
		}
	}

	/// <summary>回合开始特效（由主插件在 dice 发放后调用）。</summary>
	public static void OnRoundStart()
	{
		PlayForAllActive((profile) => profile.Events.RoundStart, BurstLife, BurstZOffset);
		PlayBeamsForAllActive((profile) => profile.Beams?.RoundStart, BurstZOffset);
	}

	/// <summary>回合结束特效（由主插件在移除 dice 之前调用）。</summary>
	public static void OnRoundEnd()
	{
		PlayForAllActive((profile) => profile.Events.RoundEnd, DeathLife, DeathZOffset);
		PlayBeamsForAllActive((profile) => profile.Beams?.RoundEnd, DeathZOffset);
	}

	/// <summary>HUD 用：把某个 dice 覆盖到的特效类别拼成中文摘要。</summary>
	public static string DescribeEffects(string? className)
	{
		if (string.IsNullOrEmpty(className) || !DiceFxTable.TryGet(className, out FxProfile p))
		{
			return string.Empty;
		}
		List<string> tags = new List<string>();
		if (!string.IsNullOrEmpty(p.Trigger.Burst)) tags.Add("触发");
		if (!string.IsNullOrEmpty(p.Trail.Particle)) tags.Add("痕迹");
		if (!string.IsNullOrEmpty(p.Attach.Particle)) tags.Add("附着");
		if (!string.IsNullOrEmpty(p.Orbit.Particle)) tags.Add("环绕");
		if (!string.IsNullOrEmpty(p.Events.Fire)) tags.Add("枪口");
		if (!string.IsNullOrEmpty(p.Events.Kill)) tags.Add("击杀");
		if (!string.IsNullOrEmpty(p.Events.KillSelf)) tags.Add("击杀者");
		if (!string.IsNullOrEmpty(p.Events.KillHeadshot)) tags.Add("爆头");
		if (!string.IsNullOrEmpty(p.Events.Hit) || !string.IsNullOrEmpty(p.Events.Hurt)) tags.Add("伤害反馈");
		if (!string.IsNullOrEmpty(p.Events.HitHeadshot)) tags.Add("爆头标记");
		if (!string.IsNullOrEmpty(p.Events.Death)) tags.Add("阵亡");
		if (!string.IsNullOrEmpty(p.Events.RoundStart)) tags.Add("回合开始");
		if (!string.IsNullOrEmpty(p.Events.RoundEnd)) tags.Add("回合结束");
		return string.Join(" · ", tags);
	}

	/// <summary>玩家离开时按 SteamID 清理（此时控制器可能已经失效，走 <c>EventPlayerDisconnect.Xuid</c>）。</summary>
	public static void OnPlayerLeft(ulong steamId)
	{
		RemovePlayerPersistent(steamId);
		Active.Remove(steamId);
		LastProc.Remove(steamId);
		LastFire.Remove(steamId);
		List<(ulong, string)> keys = new List<(ulong, string)>();
		foreach ((ulong SteamId, string ClassName) key in TrailNext.Keys)
		{
			if (key.SteamId == steamId)
			{
				keys.Add(key);
			}
		}
		foreach ((ulong, string) key in keys)
		{
			TrailNext.Remove(key);
		}
	}

	public static void ClearAll()
	{
		ClearBeams();
		ClearPersistentOnly();
		Active.Clear();
		LastProc.Clear();
		LastFire.Clear();
		TrailNext.Clear();
	}

	/// <summary>重载 dicefx.json 后调用：清掉现有持续粒子 / 光翼，让它们按新配置在下一 tick 重建。</summary>
	public static void ResetPersistent()
	{
		ClearPersistentOnly();
	}

	private static void ClearPersistentOnly()
	{
		if (Holds.Count == 0 && Orbits.Count == 0 && Winged.Count == 0)
		{
			return;
		}
		foreach (CParticleSystem system in Holds.Values)
		{
			Effects.Remove(system);
		}
		foreach (CParticleSystem system in Orbits.Values)
		{
			Effects.Remove(system);
		}
		foreach (WingFx wing in Winged.Values)
		{
			wing.Remove();
		}
		Holds.Clear();
		Orbits.Clear();
		Winged.Clear();
	}

	private static bool CanProc(ulong id, float now)
	{
		if (LastProc.TryGetValue(id, out float last) && now - last < ProcInterval)
		{
			return false;
		}
		LastProc[id] = now;
		return true;
	}

	private static void PlayForAllActive(Func<FxProfile, string?> selector, float life, float zOffset)
	{
		if (!Enabled)
		{
			return;
		}
		try
		{
			foreach (CCSPlayerController player in Utilities.GetPlayers())
			{
				if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
				{
					continue;
				}
				foreach (string className in DiceOf(player))
				{
					if (DiceFxTable.TryGet(className, out FxProfile profile))
					{
						string? particle = selector(profile);
						if (!string.IsNullOrEmpty(particle))
						{
							PlayAtPlayer(player, particle!, life, zOffset);
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void PlayAtPlayer(CCSPlayerController player, string particle, float life, float zOffset)
	{
		Vector? origin = PlayerOrigin(player, zOffset);
		if (origin == null)
		{
			return;
		}
		Effects.Play(origin, particle, life);
	}

	private static Vector? PlayerOrigin(CCSPlayerController player, float zOffset)
	{
		CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
		Vector? origin = pawn?.AbsOrigin;
		return origin == null ? null : new Vector(origin.X, origin.Y, origin.Z + zOffset);
	}

	private static void PlayBeamsForAllActive(Func<FxProfile, List<FxBeamSpec>?> selector, float zOffset)
	{
		if (!Enabled)
		{
			return;
		}
		try
		{
			foreach (CCSPlayerController player in Utilities.GetPlayers())
			{
				if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
				{
					continue;
				}
				foreach (string className in DiceOf(player))
				{
					if (DiceFxTable.TryGet(className, out FxProfile profile))
					{
						PlayBeams(selector(profile), PlayerOrigin(player, zOffset));
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void UpdateWing((ulong, string) key, FxProfile profile, Vector origin, float yaw, float now)
	{
		if (!Winged.TryGetValue(key, out WingFx? wing) || wing == null)
		{
			FxWingSlot w = profile.Wing;
			wing = new WingFx(
				BeamColor(w.Color),
				w.Width,
				w.Blades,
				w.Length,
				profile.Geom.Spread,
				profile.Geom.Back,
				profile.Geom.WingZ);
			if (wing.IsEmpty)
			{
				return;
			}
			Winged[key] = wing;
		}
		float flapSpeed = profile.Wing.Flap;
		wing.Update(origin, yaw, flapSpeed <= 0f ? 0f : now * flapSpeed * 3f);
	}

	private static void HoldPersistent(Dictionary<(ulong, string), CParticleSystem> map, (ulong, string) key, string particle, Vector position)
	{
		if (map.TryGetValue(key, out CParticleSystem? system))
		{
			if (system != null && system.IsValid)
			{
				Effects.MoveTo(system, position);
				return;
			}
			// 失效实体先移出 Managed，再重建，避免残留。
			Effects.Remove(system);
		}
		CParticleSystem? created = Effects.Play(position, particle, null);
		if (created != null)
		{
			map[key] = created;
		}
	}

	private static string? ActiveWeaponName(CCSPlayerController player)
	{
		CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return null;
		}
		CPlayer_WeaponServices? weaponServices = ((CBasePlayerPawn)pawn).WeaponServices;
		CHandle<CBasePlayerWeapon>? activeWeapon = weaponServices?.ActiveWeapon;
		CBasePlayerWeapon? weapon = activeWeapon?.Value;
		return weapon == null ? null : ((CEntityInstance)weapon).DesignerName;
	}

	private static void RemovePersistent((ulong, string) key)
	{
		if (Holds.TryGetValue(key, out CParticleSystem? hold))
		{
			Effects.Remove(hold);
			Holds.Remove(key);
		}
		if (Orbits.TryGetValue(key, out CParticleSystem? orbit))
		{
			Effects.Remove(orbit);
			Orbits.Remove(key);
		}
	}

	/// <summary>清掉某个 dice 的全部持续粒子（附着 / 环绕）与光翼。</summary>
	private static void RemovePersistentByClass(ulong steamId, string className)
	{
		List<(ulong, string)> keys = new List<(ulong, string)>();
		foreach ((ulong SteamId, string ClassName) key in Holds.Keys)
		{
			if (key.SteamId == steamId && key.ClassName == className)
			{
				keys.Add(key);
			}
		}
		foreach ((ulong SteamId, string ClassName) key in Orbits.Keys)
		{
			if (key.SteamId == steamId && key.ClassName == className)
			{
				keys.Add(key);
			}
		}
		foreach ((ulong, string) key in keys)
		{
			RemovePersistent(key);
		}
		if (Winged.TryGetValue((steamId, className), out WingFx? wing) && wing != null)
		{
			wing.Remove();
			Winged.Remove((steamId, className));
		}
	}

	private static void RemovePlayerPersistent(ulong steamId)
	{
		List<(ulong, string)> keys = new List<(ulong, string)>();
		foreach ((ulong SteamId, string ClassName) key in Holds.Keys)
		{
			if (key.SteamId == steamId) keys.Add(key);
		}
		foreach ((ulong SteamId, string ClassName) key in Orbits.Keys)
		{
			if (key.SteamId == steamId) keys.Add(key);
		}
		foreach ((ulong, string) key in keys)
		{
			RemovePersistent(key);
		}
		List<(ulong, string)> wingKeys = new List<(ulong, string)>();
		foreach ((ulong SteamId, string ClassName) key in Winged.Keys)
		{
			if (key.SteamId == steamId) wingKeys.Add(key);
		}
		foreach ((ulong, string) key in wingKeys)
		{
			if (Winged.TryGetValue(key, out WingFx? wing) && wing != null)
			{
				wing.Remove();
			}
			Winged.Remove(key);
		}
	}

	private static HashSet<string> DiceOf(CCSPlayerController player)
	{
		return Active.TryGetValue(player.SteamID, out HashSet<string> dice) ? dice : Empty;
	}

}
