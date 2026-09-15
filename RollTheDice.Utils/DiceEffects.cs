#nullable enable
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 每个 dice 的粒子/特效映射表（设计表，改特效只动本文件）。
///
/// 设计原则（2026-09-15 重做，取代旧的"常驻光环"方案）：
/// <list type="bullet">
/// <item><b>不再给玩家挂常驻光环</b>。旧版几乎每个 dice 都有一个 <see cref="Profile.Trail"/>
/// 式常驻粒子用 <c>SetParent</c> 挂在玩家身上，火系 dice 全部挂 <c>env_fire_large</c>，看起来就是
/// "一团火全局挂在身上"，又丑又挡视线。现在一律不挂。</item>
/// <item><b>两种表现方式</b>：① <see cref="Profile.Burst"/> —— 抽到/施加瞬间在玩家身上放一次（反馈）；
/// ② <see cref="Profile.Trail"/> —— 周期性在<em>脚底</em>放一个小粒子，玩家移动时自然留下"痕迹"
/// （火魔的余烬、毒刃的孢子、幽灵的雾气……），语义贴合且不遮视线。事件型 dice（枪械/雷达/金钱/进度）
/// 不挂 trail，只在击杀/受击/命中时触发。</item>
/// <item><b>事件钩子</b>：击杀（<see cref="Profile.OnKill"/> 在尸体处、<see cref="Profile.OnKillSelf"/> 在自己身上）、
/// 自己阵亡（<see cref="Profile.OnDeath"/>）、受击（<see cref="Profile.OnHurt"/>）、命中敌人（<see cref="Profile.OnHit"/>）。</item>
/// </list>
///
/// 粒子路径必须在 CS2 VPK 里真实存在（见 <see cref="ParticlePaths"/>），写错会静默失败。
/// 新增/改 dice 时在此加/改一行即可，不要在各个 dice 文件里自己 spawn 粒子（会绕过预缓存与统一清理）。
/// </summary>
public static class DiceEffects
{
	private sealed class Profile
	{
		/// <summary>抽到/施加瞬间播放一次。</summary>
		public string? Burst;

		/// <summary>周期性在脚底留下的痕迹（替代旧的常驻光环）。</summary>
		public string? Trail;

		/// <summary>Trail 的发射间隔（秒）。</summary>
		public float TrailInterval = 2f;

		/// <summary>dice 被移除时。</summary>
		public string? Remove;

		/// <summary>击杀敌人时在<em>尸体</em>处。</summary>
		public string? OnKill;

		/// <summary>击杀敌人时在<em>击杀者自己</em>身上（枪口/弹壳类）。</summary>
		public string? OnKillSelf;

		/// <summary>自己阵亡时。</summary>
		public string? OnDeath;

		/// <summary>自己受击时。</summary>
		public string? OnHurt;

		/// <summary>命中敌人时（在敌人体表触发）。</summary>
		public string? OnHit;
	}

	private static readonly Dictionary<string, Profile> Profiles = Build();

	private static readonly HashSet<string> Empty = new HashSet<string>();

	private static readonly Dictionary<ulong, HashSet<string>> Active = new Dictionary<ulong, HashSet<string>>();

	private static readonly Dictionary<ulong, float> LastProc = new Dictionary<ulong, float>();

	// 用值元组作键（steamId + dice 类名），避免每 tick 拼接字符串产生 GC 压力。
	private static readonly Dictionary<(ulong SteamId, string ClassName), float> TrailNext = new Dictionary<(ulong, string), float>();

	public static bool Enabled { get; set; } = true;

	/// <summary>只关掉脚底周期痕迹（Trail）而保留抽到/击杀等触发特效。</summary>
	public static bool TrailsEnabled { get; set; } = true;

	private const float ProcInterval = 0.25f;

	private const float BurstLife = 2f;

	private const float EventLife = 1.6f;

	private const float DeathLife = 2.2f;

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
			TrailNext.Remove((id, className));
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
				}
			}
			TrailNext.Remove((id, className));
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
	/// 脚底周期痕迹的中央驱动器：由主插件每 tick 调用一次。
	/// 只为"会持续散发东西"的 dice（火/毒/雾/鬼/自然等）在脚底留下小粒子，
	/// 按 <see cref="Profile.TrailInterval"/> 节流，不挂 Parent，玩家跑动时自然形成轨迹。
	/// </summary>
	public static void OnTick()
	{
		if (!Enabled || !TrailsEnabled || Active.Count == 0)
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
					if (!Profiles.TryGetValue(className, out Profile profile) || string.IsNullOrEmpty(profile.Trail))
					{
						continue;
					}
					(ulong, string) key = (player.SteamID, className);
					if (TrailNext.TryGetValue(key, out float next) && now < next)
					{
						continue;
					}
					TrailNext[key] = now + profile.TrailInterval;
					Effects.Play(new Vector(origin.X, origin.Y, origin.Z + TrailZOffset), profile.Trail!, profile.TrailInterval + 0.5f);
				}
			}
		}
		catch
		{
		}
	}

	public static void OnPlayerKill(CCSPlayerController? attacker, CCSPlayerController? victim)
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
			if (!string.IsNullOrEmpty(profile.OnKill) && victimPos != null)
			{
				Effects.Play(new Vector(victimPos.X, victimPos.Y, victimPos.Z + DeathZOffset), profile.OnKill!, EventLife);
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

	public static void OnPlayerDamaged(CCSPlayerController? victim, CCSPlayerController? attacker)
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
			if (CanProc(victim.SteamID, now))
			{
				foreach (string className in DiceOf(victim))
				{
					if (Profiles.TryGetValue(className, out Profile profile) && !string.IsNullOrEmpty(profile.OnHurt))
					{
						pending.Add((profile.OnHurt!, new Vector(pos.X, pos.Y, pos.Z + EventZOffset)));
					}
				}
			}
			if (attacker != null && attacker.IsValid && !Equals(attacker, victim) && CanProc(attacker.SteamID, now))
			{
				foreach (string className in DiceOf(attacker))
				{
					if (Profiles.TryGetValue(className, out Profile profile) && !string.IsNullOrEmpty(profile.OnHit))
					{
						pending.Add((profile.OnHit!, new Vector(pos.X, pos.Y, pos.Z + EventZOffset)));
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

	public static void ClearAll()
	{
		Active.Clear();
		LastProc.Clear();
		TrailNext.Clear();
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

	private static HashSet<string> DiceOf(CCSPlayerController player)
	{
		return Active.TryGetValue(player.SteamID, out HashSet<string> dice) ? dice : Empty;
	}

	private static Dictionary<string, Profile> Build()
	{
		Dictionary<string, Profile> map = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase);
		void P(string name, string? burst = null, string? trail = null, float trailInterval = 2f, string? onKill = null, string? onKillSelf = null, string? onDeath = null, string? onHurt = null, string? onHit = null, string? remove = null)
		{
			map[name] = new Profile
			{
				Burst = burst,
				Trail = trail,
				TrailInterval = trailInterval,
				Remove = remove,
				OnKill = onKill,
				OnKillSelf = onKillSelf,
				OnDeath = onDeath,
				OnHurt = onHurt,
				OnHit = onHit
			};
		}

		// ── 火 ────────────────────────────────────────────────────────────────
		// 旧版这 6 个火 dice 全都挂 env_fire_large 常驻光环 → "一团火挂在身上"。现全部改为
		// 脚底小火 trail + 事件触发，每个火 dice 触发方式不同以示区分。
		P("FireLord", burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 1.6f, onKill: ParticlePaths.FireCoverage, onDeath: ParticlePaths.MolotovExplosion);
		P("Fireball", burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 2.4f, onKill: ParticlePaths.ExplosionHegrenade, onHit: ParticlePaths.FireCoverage);
		P("FireDragon", burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 2.0f, onKill: ParticlePaths.ExplosionHegrenade, onDeath: ParticlePaths.FireLarge, onHit: ParticlePaths.FireCoverage);
		P("DragonSoul", burst: ParticlePaths.FireCoverage, trail: ParticlePaths.FireTiny, trailInterval: 2.5f, onKill: ParticlePaths.FireTiny);
		P("Dragonborn", burst: ParticlePaths.FireCoverage, trail: ParticlePaths.FireTiny, trailInterval: 2.5f, onKill: ParticlePaths.FireTiny);
		P("Corona", burst: ParticlePaths.MolotovExplosion, trail: ParticlePaths.FireTiny, trailInterval: 2.0f, onKill: ParticlePaths.FireTiny, onHit: ParticlePaths.FireCoverage);
		P("Phoenix", burst: ParticlePaths.FireLarge, onKill: ParticlePaths.FireCoverage, onDeath: ParticlePaths.FireLarge);
		P("Martyrdom", onDeath: ParticlePaths.ExplosionHegrenade, remove: ParticlePaths.ExplosionHegrenade);
		P("HotPotato", burst: ParticlePaths.C4TimerLight, trail: ParticlePaths.FireGlow, trailInterval: 1.4f, onDeath: ParticlePaths.ExplosionHegrenade);
		P("Overheat", burst: ParticlePaths.FireGlow, trail: ParticlePaths.FireGlow, trailInterval: 1.4f, onKill: ParticlePaths.FireGlow, onHurt: ParticlePaths.FireGlow);

		// ── 冰 ────────────────────────────────────────────────────────────────
		P("Frostmourne", burst: ParticlePaths.SnowBurst, trail: ParticlePaths.Snow, trailInterval: 2.2f, onKill: ParticlePaths.SnowBurst, onHit: ParticlePaths.Snow);
		P("IceDragon", burst: ParticlePaths.SnowBurst, trail: ParticlePaths.Snow, trailInterval: 2.2f, onKill: ParticlePaths.SnowBurst, onHit: ParticlePaths.SnowBurst);
		P("IceBeam", burst: ParticlePaths.SnowBurst, onHit: ParticlePaths.SnowBurst);
		P("Amber", burst: ParticlePaths.SnowBurst, onHurt: ParticlePaths.SnowBurst);

		// ── 毒 ────────────────────────────────────────────────────────────────
		P("Plague", burst: ParticlePaths.PoisonSpores, trail: ParticlePaths.PoisonSpores, trailInterval: 2.4f, onKill: ParticlePaths.PoisonSpores);
		P("PoisonBlade", burst: ParticlePaths.PoisonSpores, trail: ParticlePaths.PoisonSpores, trailInterval: 2.0f, onHit: ParticlePaths.PoisonSpores);
		P("ToxicSmoke", burst: ParticlePaths.SmokeGrenadeBody, trail: ParticlePaths.SmokePuff, trailInterval: 2.6f);
		P("Parasite", burst: ParticlePaths.PoisonSpores, trail: ParticlePaths.PoisonSpores, trailInterval: 2.8f, onKill: ParticlePaths.PoisonSpores);
		P("BoneMaggot", burst: ParticlePaths.PoisonSpores, onKill: ParticlePaths.PoisonSpores, onHit: ParticlePaths.PoisonSpores);
		P("Mosquito", burst: ParticlePaths.Blood, trail: ParticlePaths.Blood, trailInterval: 2.2f, onHit: ParticlePaths.Blood);

		// ── 血 ────────────────────────────────────────────────────────────────
		P("Vampire", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood, onHit: ParticlePaths.Blood);
		P("Berserker", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood, onHurt: ParticlePaths.Blood);
		P("HangedMan", burst: ParticlePaths.Blood, onHurt: ParticlePaths.Blood);
		P("Glutton", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		P("Taotie", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		P("DeadHand", burst: ParticlePaths.Blood, onHurt: ParticlePaths.Blood);
		P("FrontlineBeast", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		P("Sacrifice", burst: ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		P("SacrificeSelf", burst: ParticlePaths.Blood);
		P("Cupid", onKill: ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		P("Traitor", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		P("Payback", burst: ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		P("Knight", burst: ParticlePaths.Blood, onDeath: ParticlePaths.Blood);
		P("Cutter", burst: ParticlePaths.Blood, onKill: ParticlePaths.Blood);
		P("Guillotine", burst: ParticlePaths.BloodHeadshot, onKill: ParticlePaths.BloodHeadshot);
		P("Anatomist", burst: ParticlePaths.BloodHeadshot, onKill: ParticlePaths.BloodHeadshot, onHit: ParticlePaths.BloodHeadshot);
		P("PainConverter", burst: ParticlePaths.Blood, onHurt: ParticlePaths.Blood);

		// ── 电 ────────────────────────────────────────────────────────────────
		P("ThunderChain", burst: ParticlePaths.LightningStatus, onKill: ParticlePaths.LightningStatus, onHit: ParticlePaths.LightningStatus);
		P("LaserCage", burst: ParticlePaths.ElectricArc, onKill: ParticlePaths.ElectricArc);
		P("DivinePunishment", burst: ParticlePaths.LightningStatus, onKill: ParticlePaths.LightningStatus);
		P("Ragnarok", burst: ParticlePaths.StormLightning, onKill: ParticlePaths.StormLightning, onHurt: ParticlePaths.StormLightning);
		P("RadarJammer", burst: ParticlePaths.ElectricGlow, onKill: ParticlePaths.ElectricGlow);
		P("Jammer", burst: ParticlePaths.ElectricGlow, onKill: ParticlePaths.ElectricGlow);
		P("MagneticPulse", burst: ParticlePaths.ElectricGlow);
		P("Drone", burst: ParticlePaths.ElectricGlow);

		// ── 神圣 / 护甲 ───────────────────────────────────────────────────────
		P("God", burst: ParticlePaths.GoldHaloRays, onKill: ParticlePaths.GoldHaloFlare, onHurt: ParticlePaths.GoldHaloFlare);
		P("Goddess", burst: ParticlePaths.GoldHaloRays, onKill: ParticlePaths.GoldHaloFlare);
		P("Emperor", burst: ParticlePaths.GoldHaloRays, onKill: ParticlePaths.GoldHaloFlare, onHurt: ParticlePaths.GoldHaloFlare);
		P("Empress", burst: ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		P("Paladin", burst: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		P("Shield", burst: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		P("RoyalBarrier", burst: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		P("GunGod", burst: ParticlePaths.ShieldGlow, onKill: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		P("DeathKnight", burst: ParticlePaths.ShieldGlow, onKill: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ShieldGlow);
		P("DeathKnightComplete", burst: ParticlePaths.ShieldGlowHigh, onKill: ParticlePaths.ShieldGlowHigh, onHurt: ParticlePaths.ShieldGlowHigh);
		P("Gargoyle", burst: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		P("SwordSaint", burst: ParticlePaths.ShieldGlow, onHurt: ParticlePaths.ImpactArmor);
		P("GuardianAngel", burst: ParticlePaths.GoldHaloFlare, onHurt: ParticlePaths.GoldHaloFlare);
		P("DivineResurrection", burst: ParticlePaths.GoldHaloFlare, onDeath: ParticlePaths.GoldHaloFlare);
		P("Prayer", burst: ParticlePaths.GoldHaloFlare);
		P("Nirvana", burst: ParticlePaths.GoldHaloFlare);
		P("Redemption", burst: ParticlePaths.GoldHaloFlare, onDeath: ParticlePaths.GoldHaloFlare);
		P("Kinship", burst: ParticlePaths.GoldHaloFlare, onHurt: ParticlePaths.GoldHaloFlare);
		P("Yagorou", burst: ParticlePaths.GoldHaloFlare, onHurt: ParticlePaths.GoldHaloFlare);
		P("Thorns", onHurt: ParticlePaths.ImpactArmor);
		P("Evasion", onHurt: ParticlePaths.ImpactArmor);
		P("RepulsionField", burst: ParticlePaths.ImpactArmor);
		P("IronHead", onHurt: ParticlePaths.ImpactHelmet);

		// ── 金钱 / 赌 ─────────────────────────────────────────────────────────
		P("Bank", burst: ParticlePaths.MoneyBurst);
		P("Capitalist", burst: ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		P("Miser", burst: ParticlePaths.MoneyBurst);
		P("LoanShark", burst: ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		P("Bounty", burst: ParticlePaths.MoneyBurst, onKill: ParticlePaths.MoneyBurst);
		P("Pickpocket", burst: ParticlePaths.MoneyBurst, onHit: ParticlePaths.MoneyBurst);
		P("Lottery", burst: ParticlePaths.Confetti);
		P("Lucky", burst: ParticlePaths.Confetti, onKill: ParticlePaths.Confetti);
		P("RouletteGambler", burst: ParticlePaths.Confetti, onKill: ParticlePaths.Confetti);
		P("Jester", burst: ParticlePaths.Confetti, onKill: ParticlePaths.Confetti);

		// ── 隐身 / 幽灵 ───────────────────────────────────────────────────────
		P("Void", burst: ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.2f);
		P("Hermit", burst: ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.4f);
		P("ShadowWarrior", burst: ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.4f);
		P("Afterimage", burst: ParticlePaths.GhostWhisps, onHurt: ParticlePaths.GhostWhisps);
		P("FourtyTwo", burst: ParticlePaths.GhostWhisps, onHurt: ParticlePaths.GhostWhisps);
		P("Necromancer", burst: ParticlePaths.GhostWhisps, trail: ParticlePaths.GhostWhisps, trailInterval: 2.6f, onKill: ParticlePaths.GhostWhisps);
		P("SoulEater", burst: ParticlePaths.GhostWhisps, onKill: ParticlePaths.GhostWhisps);
		P("Eclipse", burst: ParticlePaths.GhostScreenGlow);
		P("Nightglow", burst: ParticlePaths.BaseGlow, trail: ParticlePaths.BaseGlow, trailInterval: 2.4f);

		// ── 烟 / 诱饵 / 哑火 ──────────────────────────────────────────────────
		P("FogOfWar", burst: ParticlePaths.SmokeGrenadeBody, trail: ParticlePaths.SmokePuff, trailInterval: 2.5f);
		P("SmokeBomb", burst: ParticlePaths.SmokeGrenadeBody, onDeath: ParticlePaths.SmokeGrenadeBody, onHurt: ParticlePaths.SmokeGrenadeBody);
		P("SmokeVision", burst: ParticlePaths.SmokeGrenadeBody);
		P("DecoyDummy", burst: ParticlePaths.DecoyGround);
		P("NoExplosives", burst: ParticlePaths.ExplosionSmokeDistort);

		// ── 爆炸 / C4 / 闪光 ──────────────────────────────────────────────────
		P("C4Expert", burst: ParticlePaths.C4TimerLight, onKill: ParticlePaths.ExplosionHegrenade, onDeath: ParticlePaths.ExplosionHegrenade);
		P("GrenadeKing", burst: ParticlePaths.ExplosionHegrenade, onKill: ParticlePaths.ExplosionHegrenade);
		P("LongerFlashes", burst: ParticlePaths.ExplosionFlashbang, onKill: ParticlePaths.ExplosionFlashbang);

		// ── 枪械（事件型，不挂 trail）：击杀时弹壳/血 ────────────────────────
		P("DeagleKing", burst: ParticlePaths.ShellDeagle, onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShellDeagle);
		P("SniperElite", burst: ParticlePaths.ShellAwp, onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShellAwp);
		P("PistolMaster", burst: ParticlePaths.ShellPistol, onKill: ParticlePaths.BloodHeadshot, onKillSelf: ParticlePaths.ShellPistol);
		P("InfiniteAmmo", burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle);
		P("ResetOnReload", burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle);
		P("ReloadGap", burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle);
		P("Synced", burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle);
		P("WeaponRoulette", burst: ParticlePaths.ShellRifle, onKillSelf: ParticlePaths.ShellRifle);
		P("NoRecoil", burst: ParticlePaths.MuzzleSpark, onKillSelf: ParticlePaths.MuzzleSpark);
		P("LastStand", burst: ParticlePaths.MuzzleSpark, onKillSelf: ParticlePaths.MuzzleSpark);
		P("Forsaken", burst: ParticlePaths.BloodHeadshot, onKill: ParticlePaths.BloodHeadshot, onHit: ParticlePaths.BloodHeadshot);
		P("Disarm", burst: ParticlePaths.ImpactMetal, onHit: ParticlePaths.ImpactMetal);

		// ── 黑洞 / 引力 / 时空 / 幻象 ─────────────────────────────────────────
		P("GravityWell", burst: ParticlePaths.DangerZoneBlack, onDeath: ParticlePaths.DangerZoneBlack);
		P("BlackHole", burst: ParticlePaths.DangerZoneBlack, onDeath: ParticlePaths.DangerZoneBlack);
		P("Singularity", burst: ParticlePaths.AmbientEmbersBlack, onKill: ParticlePaths.AmbientEmbersBlack);
		P("WhiteHole", burst: ParticlePaths.AmbientEmbersBright, onDeath: ParticlePaths.AmbientEmbersBright);
		P("NukeLeak", burst: ParticlePaths.DangerZoneLoop, onDeath: ParticlePaths.DangerZoneLoop);
		P("Cthulhu", burst: ParticlePaths.DangerZoneBlack, onKill: ParticlePaths.DangerZoneEyeball, onDeath: ParticlePaths.DangerZoneBlack);
		P("FourHorsemen", burst: ParticlePaths.DangerZoneBlack, onKill: ParticlePaths.DangerZoneBlack, onHurt: ParticlePaths.DangerZoneBlack);
		P("Teneril", burst: ParticlePaths.DangerZoneBlack, onKill: ParticlePaths.DangerZoneBlack);
		P("Curse", burst: ParticlePaths.DangerZoneBlack, onDeath: ParticlePaths.DangerZoneBlack);
		P("ChaosStorm", burst: ParticlePaths.DustDevil, trail: ParticlePaths.DustDevil, trailInterval: 2.6f);
		P("HighGravity", burst: ParticlePaths.ImpactDirt);
		P("Giant", burst: ParticlePaths.ImpactDirt, onKill: ParticlePaths.ImpactDirt);
		P("Twilight", burst: ParticlePaths.ExplosionDistort);
		P("ReverseCausality", burst: ParticlePaths.ExplosionDistort, onHit: ParticlePaths.ExplosionDistort);
		P("WASDChaos", burst: ParticlePaths.ExplosionSmokeDistort);
		P("Echo", burst: ParticlePaths.ExplosionSmokeDistort, onHit: ParticlePaths.ExplosionSmokeDistort);
		P("Trickster", burst: ParticlePaths.ExplosionSmokeDistort);
		P("InfoHole", burst: ParticlePaths.ExplosionSmokeDistort);
		P("SlyFox", burst: ParticlePaths.ExplosionSmokeDistort);
		P("Fool", burst: ParticlePaths.ExplosionSmokeDistort);
		P("Countdown", burst: ParticlePaths.WarpScreenGlow, onDeath: ParticlePaths.WarpScreenGlow);
		P("ReturnToSender", burst: ParticlePaths.WarpScreenGlow, onKill: ParticlePaths.WarpScreenGlow);
		P("BeyondHeaven", burst: ParticlePaths.WarpScreenGlow, onKill: ParticlePaths.KillBlast, remove: ParticlePaths.KillBlast);
		P("Heaven", burst: ParticlePaths.WarpScreenGlow);
		P("Izayoi", burst: ParticlePaths.WarpScreenGlow);
		P("Satellite", burst: ParticlePaths.EnergyCircle);
		P("Skyline", burst: ParticlePaths.EnergyCircle);
		P("Titanfall", burst: ParticlePaths.CopterLandDust, onDeath: ParticlePaths.CopterLandDust);

		// ── 成长 / 复活 / 回复 ────────────────────────────────────────────────
		P("Awakener", burst: ParticlePaths.ExperienceAward, onKill: ParticlePaths.ExperienceAward);
		P("Evolution", burst: ParticlePaths.ExperienceAward, onKill: ParticlePaths.ExperienceAward);
		P("InfiniteProliferation", burst: ParticlePaths.ExperienceMax, onDeath: ParticlePaths.ExperienceMax);
		P("Respawn", burst: ParticlePaths.ExperienceMax, onDeath: ParticlePaths.ExperienceMax);
		P("WheelOfFate", burst: ParticlePaths.ExperienceAward, onDeath: ParticlePaths.ExperienceAward);
		P("Reincarnation", burst: ParticlePaths.ExperienceAward, onDeath: ParticlePaths.ExperienceAward);
		P("Fate", burst: ParticlePaths.ExperienceAward, onDeath: ParticlePaths.GoldHaloFlare);
		P("World", burst: ParticlePaths.WarpScreenGlow, onDeath: ParticlePaths.WarpScreenGlow);
		P("Mimic", burst: ParticlePaths.ExperienceAward, onKill: ParticlePaths.ExperienceAward);
		P("Pope", burst: ParticlePaths.ExperienceMax, onKill: ParticlePaths.ExperienceMax);
		P("Priest", burst: ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing);
		P("Regeneration", burst: ParticlePaths.ExperienceRing, trail: ParticlePaths.ExperienceRing, trailInterval: 2.4f);
		P("JumpHeal", burst: ParticlePaths.ExperienceRing);
		P("GunHealer", burst: ParticlePaths.ExperienceRing, onHit: ParticlePaths.ExperienceRing);
		P("Rally", burst: ParticlePaths.ExperienceRing, onKill: ParticlePaths.ExperienceRing);
		P("Karma", burst: ParticlePaths.ExperienceRing, onKill: ParticlePaths.ExperienceRing);
		P("DuskDawn", burst: ParticlePaths.ExperienceRing, onHurt: ParticlePaths.ExperienceRing);
		P("Fibonacci", burst: ParticlePaths.ExperienceRing, onHurt: ParticlePaths.ExperienceRing);
		P("Crouch", burst: ParticlePaths.ExperienceRing, onHurt: ParticlePaths.ExperienceRing);
		P("Gaia", burst: ParticlePaths.Nature, trail: ParticlePaths.Nature, trailInterval: 2.6f);
		P("Wolf", burst: ParticlePaths.Nature, trail: ParticlePaths.Nature, trailInterval: 2.6f);
		P("WolfKing", burst: ParticlePaths.Nature, trail: ParticlePaths.Nature, trailInterval: 2.6f, onKill: ParticlePaths.Nature);

		// ── 标记 / 雷达 / 加速 ────────────────────────────────────────────────
		P("Tactician", burst: ParticlePaths.PingTopRings, onKill: ParticlePaths.PingTopRings);
		P("Prophet", burst: ParticlePaths.PingTopRings);
		P("Deaf", burst: ParticlePaths.PingTopRings);
		P("RadarStation", burst: ParticlePaths.PingGroundRings, trail: ParticlePaths.PingGroundRings, trailInterval: 2.0f);
		P("ImposterSyndrome", burst: ParticlePaths.PingGroundRings);
		P("Bugle", burst: ParticlePaths.FootstepDirt, trail: ParticlePaths.FootstepDirt, trailInterval: 2.2f);
		P("IncreaseSpeed", burst: ParticlePaths.FootstepDirt, trail: ParticlePaths.FootstepDirt, trailInterval: 2.0f);
		P("SpeedOnKill", burst: ParticlePaths.FootstepDirt, trail: ParticlePaths.FootstepDirt, trailInterval: 2.0f, onKill: ParticlePaths.FootstepDirt);

		// ── 其他 ──────────────────────────────────────────────────────────────
		P("DamageMultiplier", burst: ParticlePaths.ChaoticEmbers, onKill: ParticlePaths.ChaoticEmbers);
		P("Adrenaline", burst: ParticlePaths.ChaoticEmbers, onHurt: ParticlePaths.ChaoticEmbers);
		P("PlayAsChicken", burst: ParticlePaths.ChickenFeathers, onKill: ParticlePaths.ChickenFeathers, onDeath: ParticlePaths.ChickenFeathers);
		P("Combo", burst: ParticlePaths.ExperienceRollingRings, onKill: ParticlePaths.ExperienceRollingRings, onHit: ParticlePaths.ExperienceRollingRings);

		return map;
	}
}
