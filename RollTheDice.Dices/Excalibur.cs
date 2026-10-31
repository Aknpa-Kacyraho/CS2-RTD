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
/// 誓约胜利之剑 Excalibur（传说）。
/// 按住 E 蓄力、松手释放：光剑由周围光丝一段段拼凑成形并长大（脚下 Sun 法阵同步收拢、
/// 汇聚流被吸进剑体、音效渐强），松手瞬间挥落 → 倾泻一道贯穿全场的巨型金色光炮
/// （穿墙、满蓄 500 一击、命中者击退 + 白屏）。CD 90s。
/// </summary>
public class Excalibur : DiceBlueprint
{
	private const string BuffSource = "Excalibur";
	private const float HeadOffset = 40f;
	private const float BodyCenterOffset = 40f;
	private const float ChestOffset = 50f;

	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _chargeStart = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, bool> _charging = new Dictionary<ulong, bool>();
	private readonly Dictionary<ulong, ExcaliburSword> _swords = new Dictionary<ulong, ExcaliburSword>();
	private readonly Dictionary<ulong, ExcaliburBeam> _beams = new Dictionary<ulong, ExcaliburBeam>();
	private readonly Dictionary<ulong, (Vector From, Vector Dir)> _releaseInfo = new Dictionary<ulong, (Vector, Vector)>();
	private readonly Dictionary<ulong, int> _team = new Dictionary<ulong, int>();
	private readonly Dictionary<ulong, float> _scale = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, HashSet<ulong>> _hit = new Dictionary<ulong, HashSet<ulong>>();
	private readonly Dictionary<ulong, (CDynamicProp?, CDynamicProp?)> _glows = new Dictionary<ulong, (CDynamicProp?, CDynamicProp?)>();
	private readonly Dictionary<ulong, float> _nextLoop = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, bool> _readyFired = new Dictionary<ulong, bool>();
	private readonly Dictionary<ulong, float> _debugReleaseAt = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, ShockRingFx> _shocks = new Dictionary<ulong, ShockRingFx>();
	private readonly SigilTeardown _fade = new SigilTeardown();
	private static readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Excalibur";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public Excalibur(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_cooldownEnd[player.SteamID] = 0f;
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { { "playerName", player.PlayerName } });
		player.PrintToCenterAlert("⚔ 按住E蓄力，松手释放誓约胜利之剑！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player != null)
		{
			Cancel(player.SteamID);
			_cooldownEnd.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (KeyValuePair<ulong, ExcaliburSword> kv in _swords.ToList())
		{
			// ★ 一把光剑就是数百根 CBeam：交给分帧拆除队列，别在同一帧删掉
			// （实测同帧批量增删会让客户端实体表错位 → CopyExistingEntity 致命错误）。
			_fade.Add(kv.Value.RetireAll());
			CCSPlayerController p = FindBySteamId(kv.Key);
			if (p != null)
			{
				SlowUnregister(p);
			}
		}
		_swords.Clear();
		foreach (KeyValuePair<ulong, ExcaliburBeam> kv in _beams.ToList())
		{
			kv.Value.Remove();
		}
		_beams.Clear();
		// 队列留给 OnTick 分帧排空（Destroy() 才立刻清）。
		foreach (KeyValuePair<ulong, ShockRingFx> kv in _shocks.ToList())
		{
			kv.Value.Remove();
		}
		_shocks.Clear();
		foreach (KeyValuePair<ulong, (CDynamicProp?, CDynamicProp?)> kv in _glows.ToList())
		{
			GlowUtil.RemoveGlow(kv.Value.Item1, kv.Value.Item2);
		}
		_glows.Clear();
		_players.Clear();
		_cooldownEnd.Clear();
		_chargeStart.Clear();
		_charging.Clear();
		_scale.Clear();
		_hit.Clear();
		_releaseInfo.Clear();
		_team.Clear();
		_nextLoop.Clear();
		_readyFired.Clear();
		_debugReleaseAt.Clear();
	}

	public override void Destroy()
	{
		// 卸载 / 换图：没有下一 tick 了，必须立刻拆干净。
		Reset();
		_fade.Flush();
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return 0f;
		}
		ulong sid = player.SteamID;
		if (_swords.ContainsKey(sid))
		{
			return 0f;
		}
		float now = Server.CurrentTime;
		if (_cooldownEnd.TryGetValue(sid, out float cd) && now < cd)
		{
			return cd - now;
		}
		return 0f;
	}

	/// <summary>E 按下 = 起手蓄力；E 松开 = 释放（不足最短蓄力则取消）。</summary>
	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		if ((pressed & PlayerButtons.Use) != 0)
		{
			TryStartCharge(player);
		}
		else if ((released & PlayerButtons.Use) != 0)
		{
			TryRelease(player);
		}
	}

	private void TryStartCharge(CCSPlayerController player)
	{
		if (!_players.Contains(player))
		{
			return;
		}
		TryStartChargeCore(player);
	}

	private void TryStartChargeCore(CCSPlayerController player)
	{
		ulong sid = player.SteamID;
		if (_swords.ContainsKey(sid) || _charging.ContainsKey(sid))
		{
			return;
		}
		float now = Server.CurrentTime;
		if (_cooldownEnd.TryGetValue(sid, out float cd) && now < cd)
		{
			return;
		}
		StartCharge(player);
	}

	private void StartCharge(CCSPlayerController player)
	{
		ulong sid = player.SteamID;
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		Vector origin = pawn?.AbsOrigin;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0 || origin == null)
		{
			return;
		}
		ExcaliburConfig cfg = _config.Dices.Excalibur;
		float now = Server.CurrentTime;
		_chargeStart[sid] = now;
		_charging[sid] = true;
		_readyFired[sid] = false;
		_nextLoop[sid] = now;
		_swords[sid] = new ExcaliburSword(cfg, origin, now, ((int)(sid & 0x7FFFFFFF)) ^ Environment.TickCount);
		_glows[sid] = GlowUtil.CreateGlow(pawn, Color.FromArgb(255, 255, 215, 60));
		SpeedBonusManager.Register(player, BuffSource, 0f - Math.Clamp(cfg.SlowPercent, 0f, 0.95f));
		pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
		Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		try
		{
			((CBaseEntity)player).EmitSound(cfg.ChargeSound, (RecipientFilter)null, 0.85f, 0f);
		}
		catch
		{
		}
		player.PrintToCenterAlert("⚔ 蓄力中，松手倾泻！");
	}

	private void TryRelease(CCSPlayerController player)
	{
		ulong sid = player.SteamID;
		if (!_charging.ContainsKey(sid) || !_swords.ContainsKey(sid))
		{
			return;
		}
		ExcaliburConfig cfg = _config.Dices.Excalibur;
		float now = Server.CurrentTime;
		float elapsed = now - _chargeStart.GetValueOrDefault(sid, now);
		if (elapsed < MathF.Max(cfg.MinChargeSeconds, 0.05f))
		{
			Cancel(sid);
			return;
		}
		float progress = Math.Clamp(elapsed / MathF.Max(cfg.ChargeSeconds, 0.1f), 0f, 1f);
		float minScale = Math.Clamp(cfg.MinDamageScale, 0.05f, 1f);
		Release(player, minScale + (1f - minScale) * progress);
	}

	private void Release(CCSPlayerController player, float scale)
	{
		ulong sid = player.SteamID;
		if (!_swords.TryGetValue(sid, out ExcaliburSword sword))
		{
			Cancel(sid);
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		Vector origin = pawn?.AbsOrigin;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0 || origin == null)
		{
			Cancel(sid);
			return;
		}
		ExcaliburConfig cfg = _config.Dices.Excalibur;
		float now = Server.CurrentTime;
		Vector forward = Forward(pawn);
		sword.Release(origin, forward, now);
		_charging.Remove(sid);
		_readyFired.Remove(sid);
		_nextLoop.Remove(sid);
		_scale[sid] = Math.Clamp(scale, 0.05f, 1f);
		_hit[sid] = new HashSet<ulong>();
		// 对城光带的起点 / 方向在这一刻锁定（之后不跟随施法者）；队伍缓存用于施法者阵亡后仍能判伤
		_releaseInfo[sid] = (new Vector(origin.X, origin.Y, origin.Z + ChestOffset), new Vector(forward.X, forward.Y, forward.Z));
		_team[sid] = ((CBaseEntity)player).TeamNum;
		_cooldownEnd[sid] = now + MathF.Max(cfg.CooldownSeconds, 1f);
		RemoveGlow(sid);
		SlowUnregister(player);
		Effects.SoundAll(cfg.BurstSound, cfg.BurstSoundVolume);
		Effects.Shake(origin, cfg.ShakeAmplitude, cfg.ShakeFrequency, cfg.ShakeDuration, cfg.ShakeRadius);
		_shocks[sid] = new ShockRingFx(new Vector(origin.X, origin.Y, origin.Z + 2f), 3, 620f, 0.65f, Color.FromArgb(255, 255, 226, 130), MathF.Max(cfg.SigilWidth * 1.4f, 2f), now);
		player.PrintToCenterAlert("⚔ 誓约胜利之剑！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⚔ {player.PlayerName} 挥出了誓约胜利之剑！");
	}

	public void OnTick()
	{
		float now = Server.CurrentTime;
		if (_fade.Any)
		{
			_fade.Tick(now);
		}
		foreach (KeyValuePair<ulong, ShockRingFx> kv in _shocks.ToList())
		{
			kv.Value.Update(now);
			if (kv.Value.Finished)
			{
				kv.Value.Remove();
				_shocks.Remove(kv.Key);
			}
		}
		if (_debugReleaseAt.Count > 0)
		{
			foreach (KeyValuePair<ulong, float> kv in _debugReleaseAt.ToList())
			{
				if (now < kv.Value)
				{
					continue;
				}
				_debugReleaseAt.Remove(kv.Key);
				CCSPlayerController dbg = FindBySteamId(kv.Key);
				if (dbg != null)
				{
					TryRelease(dbg);
				}
			}
		}
		// 蓄力/挥落（_swords）与对城光带（_beams）是两段独立生命周期：任一非空都要继续驱动
		if (_swords.Count == 0 && _beams.Count == 0)
		{
			return;
		}
		ExcaliburConfig cfg = _config.Dices.Excalibur;
		foreach (KeyValuePair<ulong, ExcaliburSword> kv in _swords.ToList())
		{
			ulong sid = kv.Key;
			ExcaliburSword sword = kv.Value;
			CCSPlayerController player = FindBySteamId(sid);
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			Vector origin = pawn?.AbsOrigin;
			if (player == null || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0 || origin == null)
			{
				Cancel(sid);
				continue;
			}
			if (_charging.ContainsKey(sid))
			{
				float progress = Math.Clamp((now - _chargeStart.GetValueOrDefault(sid, now)) / MathF.Max(cfg.ChargeSeconds, 0.1f), 0f, 1f);
				sword.UpdateCharge(origin, Forward(pawn), progress, now);
				if (now >= _nextLoop.GetValueOrDefault(sid))
				{
					_nextLoop[sid] = now + MathF.Max(cfg.ChargeLoopInterval, 0.25f);
					try
					{
						((CBaseEntity)player).EmitSound(cfg.ChargeLoopSound, (RecipientFilter)null, 0.25f + 0.5f * progress, 0f);
					}
					catch
					{
					}
				}
				if (progress >= 1f && !_readyFired.GetValueOrDefault(sid))
				{
					_readyFired[sid] = true;
					Effects.SoundAll(cfg.ReadySound, cfg.ReadySoundVolume);
					Effects.Play(new Vector(origin.X, origin.Y, origin.Z + HeadOffset), ParticlePaths.GoldHaloRaysRadiate, 0.9f);
					player.PrintToCenterAlert("⚔ 誓约胜利之剑·就绪！");
				}
				continue;
			}
			bool swordAlive = sword.Update(origin, now);
			List<IBeamGroup> retired = sword.TakeRetired();
			if (retired.Count > 0)
			{
				_fade.Add(retired, origin);
			}
			if (!swordAlive)
			{
				// 挥落结束：剑退役，交接给「对城光带」（喷涌 + 断层光墙 + 擦地光浪 + 奔流光环）
				_swords.Remove(sid);
				sword.Remove();
				if (_releaseInfo.TryGetValue(sid, out (Vector From, Vector Dir) info))
				{
					_beams[sid] = new ExcaliburBeam(cfg, info.From, info.Dir, now);
					Effects.Play(info.From, ParticlePaths.GoldHaloRaysRadiate, 1.1f);
				}
			}
		}
		// 对城光带：喷涌 / 保持 / 淡出，期间贯穿判伤（独立于施法者存活）
		foreach (KeyValuePair<ulong, ExcaliburBeam> kv in _beams.ToList())
		{
			ulong sid = kv.Key;
			ExcaliburBeam beam = kv.Value;
			if (!beam.Update(now))
			{
				FinishBeam(sid);
				continue;
			}
			int team = _team.GetValueOrDefault(sid, -1);
			if (team < 0)
			{
				continue;
			}
			if (beam.TryGetSegment(out Vector beamStart, out Vector beamEnd))
			{
				ApplyBeamDamage(sid, team, beamStart, beamEnd, beam.CurrentWidth * 0.5f);
			}
		}
	}

	/// <summary>
	/// 对城光带贯穿判定：每个目标只结算一次，满蓄 ×scale 伤害 + 击退 + 白屏。
	/// 队友按 <c>friendly_fire_scale</c> 吃伤（默认同额友伤）；施法者自己始终免疫。
	/// </summary>
	private void ApplyBeamDamage(ulong sid, int casterTeam, Vector start, Vector end, float radius)
	{
		ExcaliburConfig cfg = _config.Dices.Excalibur;
		if (!_hit.TryGetValue(sid, out HashSet<ulong> hit))
		{
			hit = new HashSet<ulong>();
			_hit[sid] = hit;
		}
		float scale = Math.Clamp(_scale.GetValueOrDefault(sid, 1f), 0.05f, 1f);
		int baseDamage = (int)MathF.Round(MathF.Max(cfg.Damage, 0) * scale);
		if (baseDamage <= 0)
		{
			return;
		}
		float friendlyScale = Math.Clamp(cfg.FriendlyFireScale, 0f, 1f);
		Vector fwd = Vectors.Normalize(new Vector(end.X - start.X, end.Y - start.Y, end.Z - start.Z));
		float knockback = MathF.Max(cfg.KnockbackSpeed, 0f);
		foreach (CCSPlayerController victim in Utilities.GetPlayers())
		{
			if (victim == null || !victim.IsValid || victim.IsHLTV)
			{
				continue;
			}
			// 施法者自己始终免疫（光带就从自己胸口射出）
			if (victim.SteamID == sid)
			{
				continue;
			}
			bool friendly = ((CBaseEntity)victim).TeamNum == casterTeam;
			if (friendly && friendlyScale <= 0f)
			{
				continue;
			}
			if (hit.Contains(victim.SteamID))
			{
				continue;
			}
			CCSPlayerPawn vp = victim.PlayerPawn?.Value;
			Vector pos = vp?.AbsOrigin;
			if (vp == null || !vp.IsValid || ((CBaseEntity)vp).LifeState != 0 || pos == null)
			{
				continue;
			}
			Vector torso = new Vector(pos.X, pos.Y, pos.Z + BodyCenterOffset);
			if (Vectors.DistanceToSegment(torso, start, end) > radius)
			{
				continue;
			}
			if (Invulnerability.IsInvulnerable(victim))
			{
				continue;
			}
			int damage = friendly ? Math.Max((int)MathF.Round(baseDamage * friendlyScale), 1) : baseDamage;
			hit.Add(victim.SteamID);
			((CBaseEntity)vp).Health -= damage;
			Utilities.SetStateChanged((CBaseEntity)vp, "CBaseEntity", "m_iHealth", 0);
			if (knockback > 0f)
			{
				try
				{
					vp.Teleport(pos, vp.AbsRotation, new Vector(fwd.X * knockback, fwd.Y * knockback, 60f));
				}
				catch
				{
				}
			}
			Effects.Whiteout(victim, MathF.Max(cfg.BlindSeconds, 0f), cfg.BlindAlpha);
			Effects.Play(torso, ParticlePaths.GoldHaloFlare, 0.25f);
			if (((CBaseEntity)vp).Health <= 0)
			{
				Kill(vp);
			}
		}
	}

	/// <summary>打断/取消：把装置整体交给分帧拆除队列，清 glow 与减速。</summary>
	private void Cancel(ulong sid)
	{
		if (_swords.Remove(sid, out ExcaliburSword sword))
		{
			List<IBeamGroup> groups = sword.RetireAll();
			if (groups.Count > 0)
			{
				// center 参数仅占位（SigilTeardown 只用 groups）；玩家已离线时也必须入队，否则实体残留
				CCSPlayerController owner = FindBySteamId(sid);
				Vector origin = owner?.PlayerPawn?.Value?.AbsOrigin ?? new Vector(0f, 0f, 0f);
				_fade.Add(groups, origin);
			}
			sword.Remove();
		}
		_charging.Remove(sid);
		_chargeStart.Remove(sid);
		_readyFired.Remove(sid);
		_nextLoop.Remove(sid);
		_scale.Remove(sid);
		_hit.Remove(sid);
		_releaseInfo.Remove(sid);
		_team.Remove(sid);
		_debugReleaseAt.Remove(sid);
		RemoveGlow(sid);
		CCSPlayerController player = FindBySteamId(sid);
		if (player != null)
		{
			SlowUnregister(player);
		}
	}

	/// <summary>对城光带结束：清理并释放命中集合（光带独立于施法者存活，只有这里/Reset 会终止）。</summary>
	private void FinishBeam(ulong sid)
	{
		if (_beams.Remove(sid, out ExcaliburBeam beam))
		{
			beam.Remove();
		}
		_hit.Remove(sid);
		_scale.Remove(sid);
		_releaseInfo.Remove(sid);
		_team.Remove(sid);
	}

	private static Vector Forward(CCSPlayerPawn pawn)
	{
		QAngle angles = pawn.EyeAngles;
		float pitch = angles.X * MathF.PI / 180f;
		float yaw = angles.Y * MathF.PI / 180f;
		float cp = MathF.Cos(pitch);
		return new Vector(cp * MathF.Cos(yaw), cp * MathF.Sin(yaw), 0f - MathF.Sin(pitch));
	}

	private static CCSPlayerController FindBySteamId(ulong steamId)
	{
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && p.SteamID == steamId);
	}

	private static void Kill(CCSPlayerPawn pawn)
	{
		try
		{
			((CBasePlayerPawn)pawn).CommitSuicide(false, true);
		}
		catch
		{
			((CBaseEntity)pawn).Health = 0;
			Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_iHealth", 0);
		}
	}

	private void RemoveGlow(ulong sid)
	{
		if (_glows.Remove(sid, out (CDynamicProp?, CDynamicProp?) glow))
		{
			GlowUtil.RemoveGlow(glow.Item1, glow.Item2);
		}
	}

	private void SlowUnregister(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		SpeedBonusManager.Unregister(player, BuffSource);
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	/// <summary>调试：不检查持有与冷却，直接演一遍完整蓄力→倾泻（0 = 跳过蓄力按满威力立即释放）。</summary>
	public void DebugCast(CCSPlayerController player, float chargeSeconds)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		ulong sid = player.SteamID;
		if (_swords.ContainsKey(sid))
		{
			Cancel(sid);
		}
		_cooldownEnd.Remove(sid);
		StartCharge(player);
		if (!_charging.ContainsKey(sid))
		{
			return;
		}
		if (chargeSeconds <= 0.05f)
		{
			Release(player, 1f);
			return;
		}
		_debugReleaseAt[sid] = Server.CurrentTime + MathF.Max(chargeSeconds, 0.1f);
	}
}
