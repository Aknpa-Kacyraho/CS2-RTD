#nullable enable
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>特效覆盖档位：稀有度越高，自动覆盖的特效类别越多（见 <see cref="Filler"/>）。</summary>
public enum FxTier
{
	Common = 0,
	Rare = 1,
	Epic = 2,
	Legendary = 3
}

/// <summary>
/// 每个 dice 的粒子/特效映射表（设计表，改特效只动本文件）。
///
/// 设计原则（2026-09-15 重做）：
/// <list type="bullet">
/// <item><b>不挂常驻光环</b>。所有"持续附着 / 环绕"都用不挂 Parent 的粒子实体，每 tick 由
/// <see cref="OnTick"/> 重新定位（跟随稳定，也不会有 SetParent 的未知行为）。</item>
/// <item><b>特效类别</b>（按用户给的设计框架）：
///   玩家实体：<see cref="Profile.Hold"/>（持续附着 / 状态指示）、<see cref="Profile.Orbit"/>（环绕）、
///   <see cref="Profile.Trail"/>（脚底痕迹）；
///   射击战斗：<see cref="Profile.OnFire"/>（枪口/弹道）、<see cref="Profile.OnKill"/>（击杀）、
///   <see cref="Profile.OnKillSelf"/>（击杀者）、<see cref="Profile.OnKillHeadshot"/>（爆头）、
///   <see cref="Profile.OnDeath"/>（阵亡）、<see cref="Profile.OnHurt"/> / <see cref="Profile.OnHit"/> /
///   <see cref="Profile.OnHitHeadshot"/>（伤害反馈 / 命中标记）；
///   回合事件：<see cref="Profile.Burst"/>（dice 触发）、<see cref="Profile.RoundStart"/>、<see cref="Profile.RoundEnd"/>、
///   <see cref="Profile.Remove"/>（效果结束）。</item>
/// <item><b>稀有度驱动覆盖范围</b>：<b>普通 / 稀有</b>只写主粒子 theme，其余按 <see cref="Filler"/> 自动补少量类别
/// （普通=只有触发；稀有=+击杀），属"大众化"；<b>史诗 / 传说</b>则<b>逐条特调</b>——每个 dice 的每个事件都单独选贴合语义的粒子，
/// 不做 theme 自动填充，避免高稀有度 dice 之间同质化。</item>
/// </list>
///
/// 粒子路径必须在 CS2 VPK 里真实存在（见 <see cref="ParticlePaths"/>），写错会静默失败。
/// </summary>
public static class DiceEffects
{
	private sealed class Profile
	{
		public FxTier Tier;

		public string? Theme;

		// ── 玩家实体 ──
		public string? Hold;
		public float HoldZ = 40f;
		public string? Orbit;
		public float OrbitR = 70f;
		public float OrbitSpeed = 2f;
		public float OrbitZ = 40f;
		public string? Trail;
		public float TrailInterval = 2f;

		// ── 触发 ──
		public string? Burst;
		public string? Remove;

		// ── 射击 / 战斗 ──
		public string? OnFire;
		public string? OnKill;
		public string? OnKillSelf;
		public string? OnKillHeadshot;
		public string? OnDeath;
		public string? OnHurt;
		public string? OnHit;
		public string? OnHitHeadshot;

		// ── 回合 / 事件 ──
		public string? RoundStart;
		public string? RoundEnd;
	}

	private static readonly Dictionary<string, Profile> Profiles = Build();

	private static readonly HashSet<string> Empty = new HashSet<string>();

	private static readonly Dictionary<ulong, HashSet<string>> Active = new Dictionary<ulong, HashSet<string>>();

	private static readonly Dictionary<ulong, float> LastProc = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, float> LastFire = new Dictionary<ulong, float>();

	private static readonly Dictionary<(ulong SteamId, string ClassName), float> TrailNext = new Dictionary<(ulong, string), float>();

	// 持续附着 / 环绕（不挂 Parent，每 tick MoveTo 跟随）
	private static readonly Dictionary<(ulong SteamId, string ClassName), CParticleSystem> Holds = new Dictionary<(ulong, string), CParticleSystem>();
	private static readonly Dictionary<(ulong SteamId, string ClassName), CParticleSystem> Orbits = new Dictionary<(ulong, string), CParticleSystem>();

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

	private const float TrailZOffset = 4f;

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
		if (!Profiles.TryGetValue(className, out Profile profile))
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
			RemovePersistent(key);
			if (!string.IsNullOrEmpty(profile.Burst))
			{
				PlayAtPlayer(player, profile.Burst!, BurstLife, BurstZOffset);
			}
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
			RemovePersistent(key);
			if (Enabled && player.IsValid && Profiles.TryGetValue(className, out Profile profile) && !string.IsNullOrEmpty(profile.Remove))
			{
				PlayAtPlayer(player, profile.Remove!, EventLife, BurstZOffset);
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
				foreach (string className in dice)
				{
					if (!Profiles.TryGetValue(className, out Profile profile))
					{
						continue;
					}
					(ulong, string) key = (player.SteamID, className);
					if (TrailsEnabled && !string.IsNullOrEmpty(profile.Trail))
					{
						if (!TrailNext.TryGetValue(key, out float next) || now >= next)
						{
							TrailNext[key] = now + profile.TrailInterval;
							Effects.Play(new Vector(origin.X, origin.Y, origin.Z + TrailZOffset), profile.Trail!, profile.TrailInterval + 0.5f);
						}
					}
					if (TrailsEnabled && !string.IsNullOrEmpty(profile.Hold))
					{
						HoldPersistent(Holds, key, profile.Hold!, new Vector(origin.X, origin.Y, origin.Z + profile.HoldZ));
					}
					if (TrailsEnabled && !string.IsNullOrEmpty(profile.Orbit))
					{
						float phase = now * profile.OrbitSpeed + (float)(player.SteamID % 6283) / 1000f;
						Vector orbitPos = new Vector(
							origin.X + MathF.Cos(phase) * profile.OrbitR,
							origin.Y + MathF.Sin(phase) * profile.OrbitR,
							origin.Z + profile.OrbitZ);
						HoldPersistent(Orbits, key, profile.Orbit!, orbitPos);
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
				if (Profiles.TryGetValue(className, out Profile profile) && !string.IsNullOrEmpty(profile.OnFire))
				{
					Effects.PlayAtCrosshair(player, profile.OnFire!, 50f, FireLife);
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
			if (!Profiles.TryGetValue(className, out Profile profile))
			{
				continue;
			}
			string? onVictim = (headshot ? (profile.OnKillHeadshot ?? profile.OnKill) : profile.OnKill);
			if (!string.IsNullOrEmpty(onVictim) && victimPos != null)
			{
				Effects.Play(new Vector(victimPos.X, victimPos.Y, victimPos.Z + DeathZOffset), onVictim!, EventLife);
			}
			if (!string.IsNullOrEmpty(profile.OnKillSelf) && attackerPos != null)
			{
				Effects.Play(new Vector(attackerPos.X, attackerPos.Y, attackerPos.Z + BurstZOffset), profile.OnKillSelf!, EventLife);
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
			if (Profiles.TryGetValue(className, out Profile profile) && !string.IsNullOrEmpty(profile.OnDeath))
			{
				Effects.Play(new Vector(pos.X, pos.Y, pos.Z + DeathZOffset), profile.OnDeath!, DeathLife);
			}
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
					if (Profiles.TryGetValue(className, out Profile profile) && !string.IsNullOrEmpty(profile.OnHurt))
					{
						pending.Add((profile.OnHurt!, new Vector(pos.X, pos.Y, pos.Z + EventZOffset)));
					}
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
						if (!Profiles.TryGetValue(className, out Profile profile))
						{
							continue;
						}
						string? onHit = headshot ? (profile.OnHitHeadshot ?? profile.OnHit) : profile.OnHit;
						if (!string.IsNullOrEmpty(onHit))
						{
							pending.Add((onHit!, new Vector(pos.X, pos.Y, pos.Z + EventZOffset)));
						}
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
		PlayForAllActive((profile) => profile.RoundStart, BurstLife, BurstZOffset);
	}

	/// <summary>回合结束特效（由主插件在移除 dice 之前调用）。</summary>
	public static void OnRoundEnd()
	{
		PlayForAllActive((profile) => profile.RoundEnd, DeathLife, DeathZOffset);
	}

	/// <summary>HUD 用：把某个 dice 覆盖到的特效类别拼成中文摘要。</summary>
	public static string DescribeEffects(string? className)
	{
		if (string.IsNullOrEmpty(className) || !Profiles.TryGetValue(className, out Profile p))
		{
			return string.Empty;
		}
		List<string> tags = new List<string>();
		if (!string.IsNullOrEmpty(p.Burst)) tags.Add("触发");
		if (!string.IsNullOrEmpty(p.Trail)) tags.Add("痕迹");
		if (!string.IsNullOrEmpty(p.Hold)) tags.Add("附着");
		if (!string.IsNullOrEmpty(p.Orbit)) tags.Add("环绕");
		if (!string.IsNullOrEmpty(p.OnFire)) tags.Add("枪口");
		if (!string.IsNullOrEmpty(p.OnKill)) tags.Add("击杀");
		if (!string.IsNullOrEmpty(p.OnKillSelf)) tags.Add("击杀者");
		if (!string.IsNullOrEmpty(p.OnKillHeadshot)) tags.Add("爆头");
		if (!string.IsNullOrEmpty(p.OnHit) || !string.IsNullOrEmpty(p.OnHurt)) tags.Add("伤害反馈");
		if (!string.IsNullOrEmpty(p.OnHitHeadshot)) tags.Add("爆头标记");
		if (!string.IsNullOrEmpty(p.OnDeath)) tags.Add("阵亡");
		if (!string.IsNullOrEmpty(p.RoundStart)) tags.Add("回合开始");
		if (!string.IsNullOrEmpty(p.RoundEnd)) tags.Add("回合结束");
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
		ClearPersistentOnly();
		Active.Clear();
		LastProc.Clear();
		LastFire.Clear();
		TrailNext.Clear();
	}

	private static void ClearPersistentOnly()
	{
		if (Holds.Count == 0 && Orbits.Count == 0)
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
		Holds.Clear();
		Orbits.Clear();
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

	private static void PlayForAllActive(Func<Profile, string?> selector, float life, float zOffset)
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
					if (Profiles.TryGetValue(className, out Profile profile))
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
		CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
		Vector? origin = pawn?.AbsOrigin;
		if (origin == null)
		{
			return;
		}
		Effects.Play(new Vector(origin.X, origin.Y, origin.Z + zOffset), particle, life);
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
	}

	private static HashSet<string> DiceOf(CCSPlayerController player)
	{
		return Active.TryGetValue(player.SteamID, out HashSet<string> dice) ? dice : Empty;
	}

	private static Dictionary<string, Profile> Build()
	{
		Dictionary<string, Profile> map = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase);
		void A(string name, FxTier tier, string theme,
			string? burst = null, string? trail = null, float trailInterval = 2f,
			string? hold = null, float holdZ = 40f,
			string? orbit = null, float orbitR = 70f, float orbitSpeed = 2f, float orbitZ = 40f,
			string? onFire = null,
			string? onKill = null, string? onKillSelf = null, string? onKillHeadshot = null, string? onDeath = null,
			string? onHurt = null, string? onHit = null, string? onHitHeadshot = null,
			string? roundStart = null, string? roundEnd = null, string? remove = null)
		{
			map[name] = Filler(new Profile
			{
				Tier = tier,
				Theme = theme,
				Burst = burst ?? theme,
				Trail = trail,
				TrailInterval = trailInterval,
				Hold = hold,
				HoldZ = holdZ,
				Orbit = orbit,
				OrbitR = orbitR,
				OrbitSpeed = orbitSpeed,
				OrbitZ = orbitZ,
				Remove = remove,
				OnFire = onFire,
				OnKill = onKill,
				OnKillSelf = onKillSelf,
				OnKillHeadshot = onKillHeadshot,
				OnDeath = onDeath,
				OnHurt = onHurt,
				OnHit = onHit,
				OnHitHeadshot = onHitHeadshot,
				RoundStart = roundStart,
				RoundEnd = roundEnd
			});
		}

		// ── 传说 / 组合：逐条特调，每个事件用贴合该 dice 语义的粒子（不做 theme 自动填充）──
		A("Cthulhu", FxTier.Legendary, ParticlePaths.DangerZoneBlack,
			orbit: ParticlePaths.AmbientEmbersBlack,
			onKill: ParticlePaths.AmbientEmbersFalling, onKillSelf: ParticlePaths.AmbientEmbersBlack, onKillHeadshot: ParticlePaths.AmbientEmbersBright,
			onDeath: ParticlePaths.AmbientEmbersFalling, onHurt: ParticlePaths.ChaoticEmbers, onHit: ParticlePaths.ExplosionDistort, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.AmbientEmbersBlack, roundEnd: ParticlePaths.AmbientEmbersBlack);
		A("Fate", FxTier.Legendary, ParticlePaths.ExperienceAward,
			orbit: ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.ExperienceRollingRings, onKillSelf: ParticlePaths.GoldHaloFlare, onKillHeadshot: ParticlePaths.GoldHaloFlare,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.GoldHaloFlare, onHit: ParticlePaths.ExperienceRing, onHitHeadshot: ParticlePaths.GoldHaloFlare,
			roundStart: ParticlePaths.ExperienceAward, roundEnd: ParticlePaths.GoldHaloFlare);
		A("FourHorsemen", FxTier.Legendary, ParticlePaths.DangerZoneBlack,
			orbit: ParticlePaths.AmbientEmbersBlack,
			onKill: ParticlePaths.AmbientEmbersFalling, onKillSelf: ParticlePaths.AmbientEmbersBlack, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.AmbientEmbersFalling, onHurt: ParticlePaths.AmbientEmbersBlack, onHit: ParticlePaths.ExplosionDistort, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.AmbientEmbersFalling, roundEnd: ParticlePaths.AmbientEmbersBlack);
		A("God", FxTier.Legendary, ParticlePaths.GoldHaloRays,
			orbit: ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.GoldHaloFlare, onKillSelf: ParticlePaths.GoldHaloRays, onKillHeadshot: ParticlePaths.GoldHaloRays,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.ShieldGlow, onHit: ParticlePaths.GoldHaloFlare, onHitHeadshot: ParticlePaths.GoldHaloRays,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.GoldHaloRays);
		A("Ragnarok", FxTier.Legendary, ParticlePaths.StormLightning,
			orbit: ParticlePaths.ElectricGlow,
			onKill: ParticlePaths.StormLightning, onKillSelf: ParticlePaths.ElectricGlow, onKillHeadshot: ParticlePaths.ElectricArc,
			onDeath: ParticlePaths.StormLightning, onHurt: ParticlePaths.ElectricArc, onHit: ParticlePaths.ElectricArc, onHitHeadshot: ParticlePaths.ElectricArc,
			roundStart: ParticlePaths.ElectricGlow, roundEnd: ParticlePaths.ElectricGlow);
		A("WheelOfFate", FxTier.Legendary, ParticlePaths.ExperienceAward,
			orbit: ParticlePaths.ExperienceRollingRings,
			onKill: ParticlePaths.ExperienceRollingRings, onKillSelf: ParticlePaths.ExperienceAward, onKillHeadshot: ParticlePaths.ExperienceOuter,
			onDeath: ParticlePaths.ExperienceMax, onHurt: ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing, onHitHeadshot: ParticlePaths.ExperienceOuter,
			roundStart: ParticlePaths.ExperienceAward, roundEnd: ParticlePaths.ExperienceRollingRings);
		A("WolfKing", FxTier.Legendary, ParticlePaths.Nature,
			trail: ParticlePaths.Nature, trailInterval: 2.6f, orbit: ParticlePaths.Nature,
			onKill: ParticlePaths.Blood, onKillSelf: ParticlePaths.Nature, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.Nature, onHurt: ParticlePaths.Blood, onHit: ParticlePaths.Blood, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.Nature, roundEnd: ParticlePaths.Nature);
		A("World", FxTier.Legendary, ParticlePaths.EnergyCircle,
			orbit: ParticlePaths.EnergyCircle,
			onKill: ParticlePaths.ExplosionDistort, onKillSelf: ParticlePaths.EnergyCircle, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.EnergyCircle, onHurt: ParticlePaths.EnergyCircle, onHit: ParticlePaths.EnergyCircle, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.EnergyCircle, roundEnd: ParticlePaths.ExplosionDistort, remove: ParticlePaths.ExplosionDistort);
		A("BeyondHeaven", FxTier.Legendary, ParticlePaths.EnergyCircle,
			orbit: ParticlePaths.EnergyCircle,
			onKill: ParticlePaths.ExplosionDistort, onKillSelf: ParticlePaths.EnergyCircle, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.EnergyCircle, onHurt: ParticlePaths.EnergyCircle, onHit: ParticlePaths.EnergyCircle, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.EnergyCircle, roundEnd: ParticlePaths.ExplosionDistort, remove: ParticlePaths.ExplosionDistort);
		A("DeathKnightComplete", FxTier.Legendary, ParticlePaths.ShieldGlowHigh,
			hold: ParticlePaths.ShieldGlowHigh, orbit: ParticlePaths.BaseGlow,
			onKill: ParticlePaths.ShieldGlowHigh, onKillSelf: ParticlePaths.ShieldGlow, onKillHeadshot: ParticlePaths.ShieldGlowHigh,
			onDeath: ParticlePaths.ExplosionDistort, onHurt: ParticlePaths.ShieldGlowHigh, onHit: ParticlePaths.ImpactArmor, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ShieldGlowHigh, roundEnd: ParticlePaths.ShieldGlowHigh);
		A("FireDragon", FxTier.Legendary, ParticlePaths.FireTiny,
			burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 2.0f,
			onKill: ParticlePaths.ExplosionHegrenade, onKillSelf: ParticlePaths.FireTiny, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.FireLarge, onHurt: ParticlePaths.FireGlow, onHit: ParticlePaths.FireGlow, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.FireCoverage, roundEnd: ParticlePaths.FireLarge);
		A("IceDragon", FxTier.Legendary, ParticlePaths.Snow,
			burst: ParticlePaths.SnowBurst, trail: ParticlePaths.Snow, trailInterval: 2.2f,
			onKill: ParticlePaths.SnowBurst, onKillSelf: ParticlePaths.Snow, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.SnowBurst, onHurt: ParticlePaths.SnowBurst, onHit: ParticlePaths.Snow, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.Snow, roundEnd: ParticlePaths.SnowBurst);
		A("Phoenix", FxTier.Legendary, ParticlePaths.FireLarge,
			burst: ParticlePaths.FireLarge, trail: ParticlePaths.FireTiny, trailInterval: 2.5f,
			onKill: ParticlePaths.FireCoverage, onKillSelf: ParticlePaths.FireGlow, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.FireLarge, onHurt: ParticlePaths.FireGlow, onHit: ParticlePaths.FireGlow, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.FireGlow, roundEnd: ParticlePaths.FireCoverage);
		A("RadarStation", FxTier.Legendary, ParticlePaths.PingGroundRings,
			trail: ParticlePaths.EnergyCircle, trailInterval: 2.0f, orbit: ParticlePaths.EnergyCircle,
			onKill: ParticlePaths.PingTopRings, onKillSelf: ParticlePaths.EnergyCircle, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.EnergyCircle, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.PingTopRings, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.EnergyCircle, roundEnd: ParticlePaths.EnergyCircle);

		// ── 史诗：逐条特调（每个事件单独选粒子，不做 theme 自动填充）──
		A("Awakener", FxTier.Legendary, ParticlePaths.ExperienceAward,
			trail: ParticlePaths.ExperienceRing, trailInterval: 2.5f,
			onKill: ParticlePaths.ExperienceAward, onKillSelf: ParticlePaths.ExperienceRing, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ExperienceAward, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.ExperienceRing, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ExperienceAward, roundEnd: ParticlePaths.ExperienceMax);
		A("DeathKnight", FxTier.Epic, ParticlePaths.ShieldGlow,
			hold: ParticlePaths.ShieldGlow,
			onKill: ParticlePaths.ShieldGlow, onKillSelf: ParticlePaths.ShieldGlow, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ExplosionDistort, onHurt: ParticlePaths.ShieldGlow, onHit: ParticlePaths.ImpactArmor, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ShieldGlow, roundEnd: ParticlePaths.ShieldGlow);
		A("DivineResurrection", FxTier.Epic, ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.GoldHaloFlare, onKillSelf: ParticlePaths.GoldHaloRays, onKillHeadshot: ParticlePaths.GoldHaloFlare,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.ShieldGlow, onHit: ParticlePaths.GoldHaloFlare, onHitHeadshot: ParticlePaths.GoldHaloFlare,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.GoldHaloRays);
		A("Dragonborn", FxTier.Epic, ParticlePaths.FireCoverage,
			trail: ParticlePaths.FireTiny, trailInterval: 2.5f,
			onKill: ParticlePaths.FireTiny, onKillSelf: ParticlePaths.FireCoverage, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.FireLarge, onHurt: ParticlePaths.FireGlow, onHit: ParticlePaths.FireGlow, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.FireTiny, roundEnd: ParticlePaths.FireCoverage);
		A("Drone", FxTier.Epic, ParticlePaths.ElectricGlow,
			trail: ParticlePaths.ElectricTrail, trailInterval: 2.2f,
			onKill: ParticlePaths.ElectricArc, onKillSelf: ParticlePaths.ElectricGlow, onKillHeadshot: ParticlePaths.ElectricArc,
			onDeath: ParticlePaths.ElectricFollow, onHurt: ParticlePaths.ElectricGlow, onHit: ParticlePaths.ElectricArc, onHitHeadshot: ParticlePaths.ElectricArc,
			roundStart: ParticlePaths.ElectricGlow, roundEnd: ParticlePaths.ElectricGlow);
		A("Emperor", FxTier.Epic, ParticlePaths.GoldHaloRays,
			orbit: ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.GoldHaloFlare, onKillSelf: ParticlePaths.GoldHaloRays, onKillHeadshot: ParticlePaths.GoldHaloRays,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.GoldHaloFlare, onHit: ParticlePaths.GoldHaloFlare, onHitHeadshot: ParticlePaths.GoldHaloRays,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.GoldHaloRays);
		A("Forsaken", FxTier.Epic, ParticlePaths.BloodHeadshot,
			trail: ParticlePaths.GhostWhisps, trailInterval: 2.4f, onFire: ParticlePaths.MuzzleSpark,
			onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.GhostWhisps, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.Blood, onHurt: ParticlePaths.Blood, onHit: ParticlePaths.BloodHeadshot, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.GhostWhisps, roundEnd: ParticlePaths.GhostWhisps);
		A("FourtyTwo", FxTier.Epic, ParticlePaths.GhostWhisps,
			trail: ParticlePaths.GhostWhisps, trailInterval: 2.4f,
			onKill: ParticlePaths.GhostWhisps, onKillSelf: ParticlePaths.ExperienceAward, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.GhostWhisps, onHurt: ParticlePaths.GhostWhisps, onHit: ParticlePaths.GhostWhisps, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.GhostWhisps, roundEnd: ParticlePaths.ExperienceMax);
		A("GunGod", FxTier.Epic, ParticlePaths.ShieldGlow,
			hold: ParticlePaths.ShieldGlow, onFire: ParticlePaths.MuzzleSpark,
			onKill: ParticlePaths.ShieldGlow, onKillSelf: ParticlePaths.ShellRifle, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.MuzzleSpark, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ShieldGlow, roundEnd: ParticlePaths.ShieldGlow);
		A("ImposterSyndrome", FxTier.Epic, ParticlePaths.PingGroundRings,
			onKill: ParticlePaths.PingTopRings, onKillSelf: ParticlePaths.PingTopRings, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ExplosionSmokeDistort, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.ImpactArmor, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.PingGroundRings, roundEnd: ParticlePaths.ExplosionSmokeDistort);
		A("InfiniteProliferation", FxTier.Epic, ParticlePaths.ExperienceMax,
			trail: ParticlePaths.ExperienceRing, trailInterval: 2.5f,
			onKill: ParticlePaths.ExperienceAward, onKillSelf: ParticlePaths.ExperienceRing, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ExperienceMax, onHurt: ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ExperienceAward, roundEnd: ParticlePaths.ExperienceMax);
		A("Izayoi", FxTier.Epic, ParticlePaths.EnergyCircle,
			onKill: ParticlePaths.ExplosionDistort, onKillSelf: ParticlePaths.EnergyCircle, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ExplosionDistort, onHurt: ParticlePaths.EnergyCircle, onHit: ParticlePaths.EnergyCircle, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.EnergyCircle, roundEnd: ParticlePaths.EnergyCircle);
		A("Kinship", FxTier.Epic, ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.GoldHaloFlare, onKillSelf: ParticlePaths.GoldHaloFlare, onKillHeadshot: ParticlePaths.GoldHaloFlare,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.GoldHaloFlare, onHit: ParticlePaths.GoldHaloFlare, onHitHeadshot: ParticlePaths.GoldHaloFlare,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.GoldHaloFlare);
		A("Mosquito", FxTier.Epic, ParticlePaths.Blood,
			trail: ParticlePaths.Blood, trailInterval: 2.2f,
			onKill: ParticlePaths.Blood, onKillSelf: ParticlePaths.Blood, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.Blood, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.Blood, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.Blood, roundEnd: ParticlePaths.Blood);
		A("NukeLeak", FxTier.Epic, ParticlePaths.DangerZoneLoop,
			trail: ParticlePaths.AmbientEmbersFalling, trailInterval: 2.4f,
			onKill: ParticlePaths.ExplosionHegrenade, onKillSelf: ParticlePaths.AmbientEmbersBright, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.AmbientEmbersBright, onHurt: ParticlePaths.ImpactDirt, onHit: ParticlePaths.ImpactDirt, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.AmbientEmbersFalling, roundEnd: ParticlePaths.AmbientEmbersBright);
		A("Nirvana", FxTier.Epic, ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.GoldHaloFlare, onKillSelf: ParticlePaths.GoldHaloRays, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ExperienceMax, onHurt: ParticlePaths.GoldHaloFlare, onHit: ParticlePaths.GoldHaloFlare, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.GoldHaloRays);
		A("Pope", FxTier.Epic, ParticlePaths.ExperienceMax,
			orbit: ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.ExperienceMax, onKillSelf: ParticlePaths.GoldHaloRays, onKillHeadshot: ParticlePaths.GoldHaloRays,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.GoldHaloFlare, onHit: ParticlePaths.GoldHaloFlare, onHitHeadshot: ParticlePaths.GoldHaloRays,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.ExperienceMax);
		A("Prophet", FxTier.Epic, ParticlePaths.PingTopRings,
			orbit: ParticlePaths.ExperienceRollingRings,
			onKill: ParticlePaths.PingTopRings, onKillSelf: ParticlePaths.PingTopRings, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.PingGroundRings, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.ImpactArmor, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.PingTopRings, roundEnd: ParticlePaths.PingGroundRings);
		A("Reincarnation", FxTier.Epic, ParticlePaths.ExperienceAward,
			onKill: ParticlePaths.ExperienceAward, onKillSelf: ParticlePaths.ExperienceRing, onKillHeadshot: ParticlePaths.ExperienceOuter,
			onDeath: ParticlePaths.ExperienceMax, onHurt: ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing, onHitHeadshot: ParticlePaths.ExperienceOuter,
			roundStart: ParticlePaths.ExperienceAward, roundEnd: ParticlePaths.ExperienceRollingRings);
		A("RoyalBarrier", FxTier.Epic, ParticlePaths.ShieldGlow,
			hold: ParticlePaths.ShieldGlow,
			onKill: ParticlePaths.ShieldGlow, onKillSelf: ParticlePaths.ShieldGlow, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ShieldGlowHigh, onHurt: ParticlePaths.ShieldGlow, onHit: ParticlePaths.ShieldGlow, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ShieldGlow, roundEnd: ParticlePaths.ShieldGlow);
		A("Singularity", FxTier.Epic, ParticlePaths.AmbientEmbersBlack,
			orbit: ParticlePaths.AmbientEmbersBlack,
			onKill: ParticlePaths.AmbientEmbersBlack, onKillSelf: ParticlePaths.AmbientEmbersBlack, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.AmbientEmbersFalling, onHurt: ParticlePaths.AmbientEmbersBlack, onHit: ParticlePaths.ExplosionDistort, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.AmbientEmbersBlack, roundEnd: ParticlePaths.AmbientEmbersBlack);
		A("SwordSaint", FxTier.Legendary, ParticlePaths.ShieldGlow,
			onFire: ParticlePaths.MuzzleSpark,
			onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShieldGlow, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.Blood, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.Blood, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.ShieldGlow, roundEnd: ParticlePaths.ShieldGlow);
		A("Tactician", FxTier.Epic, ParticlePaths.PingTopRings,
			onKill: ParticlePaths.PingTopRings, onKillSelf: ParticlePaths.PingTopRings, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.PingGroundRings, onHurt: ParticlePaths.ImpactArmor, onHit: ParticlePaths.ImpactArmor, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.PingGroundRings, roundEnd: ParticlePaths.PingGroundRings);
		A("Taotie", FxTier.Epic, ParticlePaths.Blood,
			trail: ParticlePaths.Blood, trailInterval: 2.4f,
			onKill: ParticlePaths.Blood, onKillSelf: ParticlePaths.Blood, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.Blood, onHurt: ParticlePaths.Blood, onHit: ParticlePaths.Blood, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.Blood, roundEnd: ParticlePaths.Blood);
		A("Titanfall", FxTier.Epic, ParticlePaths.CopterLandDust,
			onKill: ParticlePaths.ImpactDirt, onKillSelf: ParticlePaths.ImpactDirt, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.ImpactDirt, onHurt: ParticlePaths.ImpactDirt, onHit: ParticlePaths.ImpactDirt, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.CopterLandDust, roundEnd: ParticlePaths.ImpactDirt);
		A("Void", FxTier.Epic, ParticlePaths.GhostWhisps,
			trail: ParticlePaths.GhostWhisps, trailInterval: 2.2f,
			onKill: ParticlePaths.GhostWhisps, onKillSelf: ParticlePaths.GhostWhisps, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.GhostWhisps, onHurt: ParticlePaths.GhostWhisps, onHit: ParticlePaths.GhostWhisps, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.GhostWhisps, roundEnd: ParticlePaths.GhostWhisps);
		A("Yagorou", FxTier.Epic, ParticlePaths.GoldHaloFlare,
			onKill: ParticlePaths.ShieldGlow, onKillSelf: ParticlePaths.GoldHaloRays, onKillHeadshot: ParticlePaths.BloodHeadshot,
			onDeath: ParticlePaths.GoldHaloRays, onHurt: ParticlePaths.ShieldGlow, onHit: ParticlePaths.ImpactArmor, onHitHeadshot: ParticlePaths.BloodHeadshot,
			roundStart: ParticlePaths.GoldHaloFlare, roundEnd: ParticlePaths.GoldHaloRays);

		// ── 稀有（+ 击杀）──
		A("Afterimage", FxTier.Rare, ParticlePaths.GhostWhisps, onHurt: ParticlePaths.GhostWhisps);
		A("Berserker", FxTier.Rare, ParticlePaths.Blood, trail: ParticlePaths.Blood, trailInterval: 2.2f, onHurt: ParticlePaths.Blood);
		A("BlackHole", FxTier.Rare, ParticlePaths.DangerZoneBlack, onDeath: ParticlePaths.AmbientEmbersFalling);
		A("BoneMaggot", FxTier.Rare, ParticlePaths.PoisonSpores, onHit: ParticlePaths.PoisonSpores);
		A("C4Expert", FxTier.Rare, ParticlePaths.C4TimerLight, onKill: ParticlePaths.ExplosionHegrenade, onDeath: ParticlePaths.ExplosionHegrenade);
		A("ChaosStorm", FxTier.Rare, ParticlePaths.DustDevil, trail: ParticlePaths.DustDevil, trailInterval: 2.6f);
		A("Combo", FxTier.Rare, ParticlePaths.ExperienceRollingRings, onHit: ParticlePaths.ExperienceRollingRings);
		A("Countdown", FxTier.Rare, ParticlePaths.ExplosionDistort, onDeath: ParticlePaths.ExplosionDistort);
		A("DeagleKing", FxTier.Rare, ParticlePaths.BloodHeadshot, burst: ParticlePaths.ShellDeagle, onFire: ParticlePaths.MuzzlePistol, onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShellDeagle);
		A("DivinePunishment", FxTier.Rare, ParticlePaths.LightningStatus, onKill: ParticlePaths.LightningStatus);
		A("DragonSoul", FxTier.Rare, ParticlePaths.FireCoverage, trail: ParticlePaths.FireTiny, trailInterval: 2.5f, onKill: ParticlePaths.FireTiny);
		A("DuskDawn", FxTier.Rare, ParticlePaths.ExperienceRing, onHurt: ParticlePaths.ExperienceRing);
		A("Empress", FxTier.Rare, ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		A("Evasion", FxTier.Rare, ParticlePaths.ImpactArmor, onHurt: ParticlePaths.ImpactArmor);
		A("Fibonacci", FxTier.Rare, ParticlePaths.ExperienceRing, onHurt: ParticlePaths.ExperienceRing);
		A("FireLord", FxTier.Rare, ParticlePaths.FireTiny, burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 1.6f, onKill: ParticlePaths.FireCoverage, onDeath: ParticlePaths.MolotovExplosion);
		A("Fool", FxTier.Rare, ParticlePaths.ExplosionSmokeDistort);
		A("FrontlineBeast", FxTier.Rare, ParticlePaths.Blood, trail: ParticlePaths.Blood, trailInterval: 2.4f, onKill: ParticlePaths.Blood);
		A("Giant", FxTier.Rare, ParticlePaths.ImpactDirt, onKill: ParticlePaths.ImpactDirt);
		A("Glutton", FxTier.Rare, ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		A("Goddess", FxTier.Rare, ParticlePaths.GoldHaloRays, onKill: ParticlePaths.GoldHaloFlare);
		A("GravityWell", FxTier.Rare, ParticlePaths.DangerZoneBlack, onDeath: ParticlePaths.AmbientEmbersFalling);
		A("GrenadeKing", FxTier.Rare, ParticlePaths.ExplosionHegrenade, onKill: ParticlePaths.ExplosionHegrenade);
		A("GuardianAngel", FxTier.Rare, ParticlePaths.GoldHaloFlare, onHurt: ParticlePaths.GoldHaloFlare);
		A("Guillotine", FxTier.Rare, ParticlePaths.BloodHeadshot, onKill: ParticlePaths.BloodHeadshot);
		A("Heaven", FxTier.Rare, ParticlePaths.EnergyCircle);
		A("Hermit", FxTier.Rare, ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.4f);
		A("IceBeam", FxTier.Rare, ParticlePaths.Snow, burst: ParticlePaths.SnowBurst, onHit: ParticlePaths.SnowBurst);
		A("Knight", FxTier.Rare, ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		A("LaserCage", FxTier.Rare, ParticlePaths.ElectricArc, onKill: ParticlePaths.ElectricArc);
		A("MagneticPulse", FxTier.Rare, ParticlePaths.ElectricGlow);
		A("Martyrdom", FxTier.Rare, ParticlePaths.ExplosionHegrenade, burst: ParticlePaths.C4TimerLight, onDeath: ParticlePaths.ExplosionHegrenade, remove: ParticlePaths.ExplosionHegrenade);
		A("Mimic", FxTier.Rare, ParticlePaths.ExperienceAward, onKill: ParticlePaths.ExperienceAward);
		A("Necromancer", FxTier.Rare, ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.6f, onKill: ParticlePaths.GhostWhisps);
		A("Nightglow", FxTier.Rare, ParticlePaths.BaseGlow, trail: ParticlePaths.BaseGlow, trailInterval: 2.4f);
		A("PainConverter", FxTier.Rare, ParticlePaths.Blood, onHurt: ParticlePaths.Blood);
		A("Parasite", FxTier.Rare, ParticlePaths.PoisonSpores, trail: ParticlePaths.PoisonSpores, trailInterval: 2.8f, onKill: ParticlePaths.PoisonSpores);
		A("PlayAsChicken", FxTier.Rare, ParticlePaths.ChickenFeathers, onKill: ParticlePaths.ChickenFeathers, onDeath: ParticlePaths.ChickenFeathers);
		A("Prayer", FxTier.Rare, ParticlePaths.GoldHaloFlare);
		A("Rally", FxTier.Rare, ParticlePaths.ExperienceRing, onKill: ParticlePaths.ExperienceRing);
		A("RepulsionField", FxTier.Rare, ParticlePaths.ImpactArmor);
		A("ResetOnReload", FxTier.Rare, ParticlePaths.ShellRifle, burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle, onFire: ParticlePaths.MuzzleSpark);
		A("Respawn", FxTier.Rare, ParticlePaths.ExperienceMax, onDeath: ParticlePaths.ExperienceMax);
		A("ReturnToSender", FxTier.Rare, ParticlePaths.ExplosionDistort, onKill: ParticlePaths.ExplosionDistort);
		A("ReverseCausality", FxTier.Rare, ParticlePaths.ExplosionDistort, onHit: ParticlePaths.ImpactArmor);
		A("RouletteGambler", FxTier.Rare, ParticlePaths.Confetti, onKill: ParticlePaths.Confetti);
		A("Sacrifice", FxTier.Rare, ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		A("Satellite", FxTier.Rare, ParticlePaths.EnergyCircle);
		A("Shield", FxTier.Rare, ParticlePaths.ShieldGlow, hold: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		A("Skyline", FxTier.Rare, ParticlePaths.EnergyCircle);
		A("SmokeBomb", FxTier.Common, ParticlePaths.SmokeGrenadeBody, onDeath: ParticlePaths.SmokeGrenadeBody, onHurt: ParticlePaths.SmokeGrenadeBody);
		A("SmokeVision", FxTier.Rare, ParticlePaths.SmokeGrenadeBody);
		A("SniperElite", FxTier.Rare, ParticlePaths.BloodHeadshot, burst: ParticlePaths.ShellAwp, onFire: ParticlePaths.MuzzleSpark, onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShellAwp);
		A("SoulEater", FxTier.Rare, ParticlePaths.GhostWhisps, onKill: ParticlePaths.GhostWhisps);
		A("SpeedOnKill", FxTier.Rare, ParticlePaths.FootstepDirt, trail: ParticlePaths.FootstepDirt, trailInterval: 2.0f, onKill: ParticlePaths.FootstepDirt);
		A("Twilight", FxTier.Rare, ParticlePaths.ExplosionDistort);
		A("Vampire", FxTier.Rare, ParticlePaths.Blood, onKill: ParticlePaths.Blood, onHit: ParticlePaths.Blood);
		A("WASDChaos", FxTier.Rare, ParticlePaths.ExplosionSmokeDistort);
		A("WhiteHole", FxTier.Rare, ParticlePaths.AmbientEmbersBright, onDeath: ParticlePaths.AmbientEmbersBright);

		// ── 普通（只有触发瞬间 + 少量显式事件）──
		A("Adrenaline", FxTier.Common, ParticlePaths.ChaoticEmbers, onHurt: ParticlePaths.ChaoticEmbers);
		A("Amber", FxTier.Common, ParticlePaths.SnowBurst, onHurt: ParticlePaths.SnowBurst);
		A("Anatomist", FxTier.Common, ParticlePaths.BloodHeadshot, onKill: ParticlePaths.BloodHeadshot, onHit: ParticlePaths.BloodHeadshot);
		A("Bank", FxTier.Common, ParticlePaths.MoneyBurst);
		A("Bounty", FxTier.Common, ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		A("Bugle", FxTier.Common, ParticlePaths.FootstepDirt, trail: ParticlePaths.FootstepDirt, trailInterval: 2.2f);
		A("Capitalist", FxTier.Common, ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		A("Corona", FxTier.Common, ParticlePaths.FireTiny, burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 2.0f, onKill: ParticlePaths.FireTiny, onHit: ParticlePaths.FireGlow);
		A("Crouch", FxTier.Common, ParticlePaths.ExperienceRing, onHurt: ParticlePaths.ExperienceRing);
		A("Cupid", FxTier.Common, ParticlePaths.Blood, onKill: ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		A("Curse", FxTier.Common, ParticlePaths.AmbientEmbersBlack, onDeath: ParticlePaths.AmbientEmbersFalling);
		A("Cutter", FxTier.Common, ParticlePaths.Blood, trail: ParticlePaths.Blood, trailInterval: 2.2f, onKill: ParticlePaths.Blood);
		A("DamageMultiplier", FxTier.Common, ParticlePaths.ChaoticEmbers, onKill: ParticlePaths.ChaoticEmbers);
		A("DeadHand", FxTier.Common, ParticlePaths.Blood, onHurt: ParticlePaths.Blood);
		A("Deaf", FxTier.Common, ParticlePaths.PingTopRings);
		A("DecoyDummy", FxTier.Common, ParticlePaths.DecoyGround);
		A("Disarm", FxTier.Rare, ParticlePaths.ImpactMetal, onHit: ParticlePaths.ImpactMetal);
		A("Echo", FxTier.Common, ParticlePaths.ExplosionSmokeDistort, onHit: ParticlePaths.ImpactArmor);
		A("Eclipse", FxTier.Common, ParticlePaths.AmbientEmbersBlack);
		A("Evolution", FxTier.Common, ParticlePaths.ExperienceAward, onKill: ParticlePaths.ExperienceAward);
		A("Fireball", FxTier.Common, ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 2.4f, onKill: ParticlePaths.ExplosionHegrenade, onHit: ParticlePaths.FireGlow);
		A("FogOfWar", FxTier.Common, ParticlePaths.SmokeGrenadeBody, trail: ParticlePaths.SmokePuff, trailInterval: 2.5f);
		A("Frostmourne", FxTier.Common, ParticlePaths.Snow, burst: ParticlePaths.SnowBurst, trail: ParticlePaths.Snow, trailInterval: 2.2f, onKill: ParticlePaths.SnowBurst, onHit: ParticlePaths.Snow);
		A("Gaia", FxTier.Common, ParticlePaths.Nature, trail: ParticlePaths.Nature, trailInterval: 2.6f);
		A("Gargoyle", FxTier.Common, ParticlePaths.ShieldGlow, hold: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		A("GunHealer", FxTier.Common, ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing);
		A("HangedMan", FxTier.Common, ParticlePaths.Blood, onHurt: ParticlePaths.Blood);
		A("HighGravity", FxTier.Common, ParticlePaths.ImpactDirt);
		A("HotPotato", FxTier.Common, ParticlePaths.FireGlow, burst: ParticlePaths.C4TimerLight, trail: ParticlePaths.FireGlow, trailInterval: 1.4f, onDeath: ParticlePaths.ExplosionHegrenade);
		A("IncreaseSpeed", FxTier.Common, ParticlePaths.FootstepDirt, trail: ParticlePaths.FootstepDirt, trailInterval: 2.0f);
		A("InfiniteAmmo", FxTier.Common, ParticlePaths.ShellRifle, burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle, onFire: ParticlePaths.MuzzleSpark);
		A("InfoHole", FxTier.Common, ParticlePaths.ExplosionSmokeDistort);
		A("IronHead", FxTier.Common, ParticlePaths.ImpactHelmet, onHurt: ParticlePaths.ImpactHelmet);
		A("Jammer", FxTier.Common, ParticlePaths.ElectricGlow, onKill: ParticlePaths.ElectricGlow);
		A("Jester", FxTier.Common, ParticlePaths.Confetti, onKill: ParticlePaths.Confetti);
		A("JumpHeal", FxTier.Common, ParticlePaths.ExperienceRing);
		A("Karma", FxTier.Common, ParticlePaths.ExperienceRing, onKill: ParticlePaths.ExperienceRing);
		A("LastStand", FxTier.Common, ParticlePaths.MuzzleSpark, burst: ParticlePaths.MuzzleSpark, onKillSelf: ParticlePaths.MuzzleSpark, onFire: ParticlePaths.MuzzleSpark);
		A("LoanShark", FxTier.Common, ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		A("LongerFlashes", FxTier.Common, ParticlePaths.ExplosionFlashbang, onKill: ParticlePaths.ExplosionDistort);
		A("Lottery", FxTier.Common, ParticlePaths.Confetti);
		A("Lucky", FxTier.Common, ParticlePaths.Confetti, onKill: ParticlePaths.Confetti);
		A("Miser", FxTier.Common, ParticlePaths.MoneyBurst);
		A("NoExplosives", FxTier.Common, ParticlePaths.ExplosionSmokeDistort);
		A("NoRecoil", FxTier.Common, ParticlePaths.MuzzleSpark, burst: ParticlePaths.MuzzleSpark, onKillSelf: ParticlePaths.MuzzleSpark, onFire: ParticlePaths.MuzzleSpark);
		A("Overheat", FxTier.Common, ParticlePaths.FireGlow, trail: ParticlePaths.FireGlow, trailInterval: 1.4f, onKill: ParticlePaths.FireGlow, onHurt: ParticlePaths.FireGlow);
		A("Paladin", FxTier.Common, ParticlePaths.ShieldGlow, hold: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		A("Payback", FxTier.Common, ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		A("Pickpocket", FxTier.Common, ParticlePaths.MoneyBurst, onHit: ParticlePaths.MoneyBurst);
		A("PistolMaster", FxTier.Common, ParticlePaths.ShellPistol, burst: ParticlePaths.ShellPistol, onFire: ParticlePaths.MuzzlePistol, onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShellPistol);
		A("Plague", FxTier.Common, ParticlePaths.PoisonSpores, trail: ParticlePaths.PoisonSpores, trailInterval: 2.4f, onKill: ParticlePaths.PoisonSpores);
		A("PoisonBlade", FxTier.Common, ParticlePaths.PoisonSpores, trail: ParticlePaths.PoisonSpores, trailInterval: 2.0f, onHit: ParticlePaths.PoisonSpores);
		A("Priest", FxTier.Common, ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing);
		A("RadarJammer", FxTier.Common, ParticlePaths.ElectricGlow, onKill: ParticlePaths.ElectricGlow);
		A("Redemption", FxTier.Common, ParticlePaths.GoldHaloFlare, onDeath: ParticlePaths.GoldHaloFlare);
		A("Regeneration", FxTier.Common, ParticlePaths.ExperienceRing, trail: ParticlePaths.ExperienceRing, trailInterval: 2.4f);
		A("ReloadGap", FxTier.Common, ParticlePaths.ShellRifle, burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle, onFire: ParticlePaths.MuzzleSpark);
		A("SacrificeSelf", FxTier.Common, ParticlePaths.Blood);
		A("ShadowWarrior", FxTier.Common, ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.4f);
		A("SlyFox", FxTier.Common, ParticlePaths.ExplosionSmokeDistort);
		A("Synced", FxTier.Rare, ParticlePaths.ShellRifle, burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle, onFire: ParticlePaths.MuzzleSpark);
		A("Teneril", FxTier.Common, ParticlePaths.AmbientEmbersBlack, onKill: ParticlePaths.GhostWhisps);
		A("Thorns", FxTier.Common, ParticlePaths.ImpactArmor, onHurt: ParticlePaths.ImpactArmor);
		A("ThunderChain", FxTier.Common, ParticlePaths.LightningStatus, onKill: ParticlePaths.LightningStatus, onHit: ParticlePaths.LightningStatus);
		A("ToxicSmoke", FxTier.Common, ParticlePaths.SmokeGrenadeBody, trail: ParticlePaths.SmokePuff, trailInterval: 2.6f);
		A("Traitor", FxTier.Common, ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		A("Trickster", FxTier.Common, ParticlePaths.ExplosionSmokeDistort);
		A("WeaponRoulette", FxTier.Common, ParticlePaths.ShellRifle, burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle, onFire: ParticlePaths.MuzzleSpark);
		A("Wolf", FxTier.Common, ParticlePaths.Nature, trail: ParticlePaths.Nature, trailInterval: 2.6f);

		return map;
	}

	/// <summary>
	/// 只给"普通 / 稀有"按 theme 自动补齐少量类别（大众化即可）。
	/// <b>史诗 / 传说一律逐条显式指定（特调）</b>，这里不做任何 theme 自动填充——
	/// 否则高稀有度 dice 会退化成"同一个主题粒子到处用"的同质化特效。
	/// </summary>
	private static Profile Filler(Profile p)
	{
		if (p.Tier == FxTier.Rare)
		{
			p.OnKill ??= p.Theme;
		}
		return p;
	}
}
