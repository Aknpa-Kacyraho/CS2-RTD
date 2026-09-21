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
/// 坠落天空（内部名 FinalJudgment，传说）。
///
/// <para>按 E 两阶段展开：① 落点长出**苍白色立体穹顶**（纬度自下而上生长、内部符文带流转，
/// 倒数末尾亮度脉冲）；② 头顶高空**由低到高弹入数十个蓝白法阵**，倒数末尾向内合拢蓄力。
/// 20s 后从阵群中心贯下**通天光柱**（≈50m 直径，多层同心近似径向渐变）+ 地面冲击环 + 结晶/爆炸粒子，
/// 以落点为球心、半径 <c>falloff_radius</c>（≈50m）内按距离 2000→250 衰减，范围外无伤。</para>
///
/// <para>落点按 E 时**锁定不跟随**（施法者可以走出穹顶）。施法后即使施法者阵亡也会照常引爆。</para>
/// </summary>
public class FinalJudgment : DiceBlueprint
{
	// 阶段一：苍白色穹顶。
	private static readonly Color DomeCore = Color.FromArgb(255, 228, 242, 255);
	private static readonly Color DomeMid = Color.FromArgb(255, 188, 218, 245);
	private static readonly Color DomeHalo = Color.FromArgb(255, 92, 140, 190);

	// 阶段二：蓝白天空法阵 / 通天光柱。
	private static readonly Color IceCore = Color.FromArgb(255, 245, 252, 255);
	private static readonly Color IceMid = Color.FromArgb(255, 158, 214, 255);
	private static readonly Color IceHalo = Color.FromArgb(255, 66, 126, 198);

	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _castAt = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _detonateAt = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, Vector> _center = new Dictionary<ulong, Vector>();
	private readonly Dictionary<ulong, SigilDome> _dome = new Dictionary<ulong, SigilDome>();
	private readonly Dictionary<ulong, SkySigilField> _sky = new Dictionary<ulong, SkySigilField>();
	private readonly Dictionary<ulong, ShockRingFx> _shock = new Dictionary<ulong, ShockRingFx>();
	private readonly Dictionary<ulong, int> _nextSecond = new Dictionary<ulong, int>();
	private readonly SigilTeardown _fade = new SigilTeardown();

	public override string ClassName => "FinalJudgment";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public FinalJudgment(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		player.PrintToCenterAlert("☄ 按E发动坠落天空！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		// 故意不 ClearPending：20s 太长，施法者阵亡也必须照常引爆（否则就等于没伤害）。
		if (player != null)
		{
			_cooldownEnd.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		_fade.Flush();
		foreach (SigilDome dome in _dome.Values)
		{
			dome.Remove();
		}
		foreach (SkySigilField sky in _sky.Values)
		{
			sky.Remove();
		}
		foreach (ShockRingFx shock in _shock.Values)
		{
			shock.Remove();
		}
		_players.Clear();
		_cooldownEnd.Clear();
		_castAt.Clear();
		_detonateAt.Clear();
		_center.Clear();
		_dome.Clear();
		_sky.Clear();
		_shock.Clear();
		_nextSecond.Clear();
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
		if (_detonateAt.ContainsKey(player.SteamID))
		{
			return 0f;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cd) && now < cd)
		{
			return cd - now;
		}
		return 0f;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (player == null || !player.IsValid || (pressed & PlayerButtons.Use) == 0 || !_players.Contains(player))
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		if (_detonateAt.ContainsKey(player.SteamID))
		{
			return;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cd) && now < cd)
		{
			return;
		}
		Vector origin = pawn.AbsOrigin;
		if (origin == null)
		{
			return;
		}
		Vector center = new Vector(origin.X, origin.Y, origin.Z);
		FinalJudgmentConfig cfg = _config.Dices.FinalJudgment;
		ulong sid = player.SteamID;
		SigilParams sp = SigilParams.From(cfg.SigilDensity, cfg.RuneTicks, cfg.TickRingCount, cfg.StarPoints, cfg.StarSkip, cfg.PolygonSides, cfg.DoubleLine, cfg.SigilSeed);

		if (cfg.DomeEnabled)
		{
			SigilPalette domePalette = new SigilPalette(DomeCore, DomeMid, DomeHalo);
			_dome[sid] = new SigilDome(center, cfg.DomeRadius, domePalette, cfg.SigilWidth, cfg.DomeLatitudeRings, cfg.DomeSegments, cfg.DomeMeridians, 8, cfg.DomeSpin, cfg.DomeGrowSeconds, now);
		}
		if (cfg.SkyEnabled)
		{
			SigilPalette skyPalette = new SigilPalette(IceCore, IceMid, IceHalo);
			_sky[sid] = new SkySigilField(center, cfg.SkyCount, cfg.SkyHeightStart, cfg.SkyHeightEnd, cfg.SkyRadiusStart, cfg.SkyRadiusEnd, cfg.SkyRadiusAlternate, cfg.SkyStartDelay, cfg.SkyLayerDelay, cfg.SkyGrowSeconds, cfg.SkySpin, sp, skyPalette, cfg.SigilWidth, now);
		}

		Effects.Play(new Vector(center.X, center.Y, center.Z + 2f), ParticlePaths.PingGroundRings, cfg.DelaySeconds + 1.5f);
		_castAt[sid] = now;
		_detonateAt[sid] = now + cfg.DelaySeconds;
		_center[sid] = center;
		_nextSecond[sid] = (int)MathF.Ceiling(cfg.DelaySeconds);
		_cooldownEnd[sid] = now + cfg.CooldownSeconds;
		player.PrintToCenterAlert($"☄ 坠落天空！{cfg.DelaySeconds:0}s 后降临，快离开中心！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☄ {player.PlayerName} 发动了坠落天空！");
	}

	public void OnTick()
	{
		float now = Server.CurrentTime;
		if (_fade.Any)
		{
			_fade.Tick(now);
		}
		foreach (KeyValuePair<ulong, ShockRingFx> kv in _shock.ToList())
		{
			kv.Value.Update(now);
			if (kv.Value.Finished)
			{
				kv.Value.Remove();
				_shock.Remove(kv.Key);
			}
		}
		if (_detonateAt.Count == 0)
		{
			return;
		}
		FinalJudgmentConfig cfg = _config.Dices.FinalJudgment;
		float delay = MathF.Max(cfg.DelaySeconds, 0.1f);
		foreach (KeyValuePair<ulong, float> kv in _detonateAt.ToList())
		{
			ulong sid = kv.Key;
			Vector center = _center.TryGetValue(sid, out Vector c) ? c : null;
			if (center == null)
			{
				ClearPending(sid);
				continue;
			}
			float remain = MathF.Max(kv.Value - now, 0f);

			if (_dome.TryGetValue(sid, out SigilDome dome))
			{
				dome.Update(center, now, 1f);
				float pulse = 0f;
				if (cfg.DomePulseSeconds > 0f && remain <= cfg.DomePulseSeconds)
				{
					pulse = Math.Clamp(1f - remain / cfg.DomePulseSeconds, 0f, 1f);
				}
				dome.SetPulse(pulse);
			}
			if (_sky.TryGetValue(sid, out SkySigilField sky))
			{
				float contract = 1f;
				float contractSeconds = MathF.Max(cfg.SkyContractSeconds, 0.1f);
				if (remain < contractSeconds)
				{
					contract = 1f - cfg.SkyContract * (1f - remain / contractSeconds);
				}
				sky.Update(center, now, contract);
			}

			if (now >= kv.Value)
			{
				DetachSigil(sid, center);
				Detonate(sid, center);
				continue;
			}
			int remaining = (int)MathF.Ceiling(kv.Value - now);
			if (_nextSecond.TryGetValue(sid, out int last) && remaining < last)
			{
				_nextSecond[sid] = remaining;
				FindBySteamId(sid)?.PrintToCenterAlert($"☄ 坠落天空 {remaining}s");
			}
		}
	}

	private void Detonate(ulong casterSid, Vector center)
	{
		FinalJudgmentConfig cfg = _config.Dices.FinalJudgment;
		float radius = MathF.Max(cfg.FalloffRadius, 1f);
		CCSPlayerController caster = FindBySteamId(casterSid);
		if (caster != null && DiceSynergy.HasPartner(caster, "Ragnarok"))
		{
			radius *= 1.3f;
		}

		// 通天光柱：多层同心 CBeam 近似"炽白核心 → 蓝白边缘"。
		Effects.SkyPillar(new Vector(center.X, center.Y, center.Z), cfg.PillarHeight, IceCore, IceMid, IceHalo, cfg.PillarRadius, cfg.PillarLife);
		Effects.Play(center, ParticlePaths.ExplosionHegrenade, 2f);
		Effects.Play(center, ParticlePaths.ExplosionDistort, 2f);
		Effects.Play(center, ParticlePaths.ExplosionFlashbang, 2f);
		Effects.Play(center, ParticlePaths.SnowBurst, 2f);
		Effects.Explosion(center, 0, null);
		Effects.SoundAll(cfg.ExplosionSound, cfg.ExplosionSoundVolume);
		Effects.Shake(center, cfg.ShakeAmplitude, cfg.ShakeFrequency, cfg.ShakeDuration, cfg.ShakeRadius);

		// 地面冲击环：从落点向外扩散（每 tick 在 OnTick 里 Update）。
		if (_shock.TryGetValue(casterSid, out ShockRingFx existing))
		{
			existing.Remove();
		}
		_shock[casterSid] = new ShockRingFx(center, cfg.ShockRings, cfg.ShockRadius, cfg.ShockSeconds, IceMid, cfg.SigilWidth * 1.5f, Server.CurrentTime);

		// 范围伤害：落点球心、radius 内 2000→250 线性衰减；范围外无伤。施法者自己也在范围内。
		foreach (CCSPlayerController victim in Utilities.GetPlayers())
		{
			if (victim == null || !victim.IsValid || victim.IsHLTV)
			{
				continue;
			}
			CCSPlayerPawn vp = victim.PlayerPawn?.Value;
			Vector pos = vp?.AbsOrigin;
			if (vp == null || !vp.IsValid || ((CBaseEntity)vp).LifeState != 0 || pos == null)
			{
				continue;
			}
			float dx = pos.X - center.X;
			float dy = pos.Y - center.Y;
			float dz = pos.Z - center.Z;
			float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
			float proximity = Math.Clamp(1f - dist / radius, 0f, 1f);
			if (proximity <= 0f)
			{
				continue;
			}
			Effects.Whiteout(victim, cfg.WhiteoutSeconds * proximity, 255f * proximity);
			if (Invulnerability.IsInvulnerable(victim))
			{
				continue;
			}
			int dmg = cfg.FalloffEnabled
				? cfg.MinDamage + (int)MathF.Round((cfg.MaxDamage - cfg.MinDamage) * proximity)
				: cfg.MaxDamage;
			if (dmg <= 0)
			{
				continue;
			}
			((CBaseEntity)vp).Health -= dmg;
			Utilities.SetStateChanged((CBaseEntity)vp, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)vp).Health <= 0)
			{
				Kill(vp);
			}
		}
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

	/// <summary>引爆前把穹顶 / 法阵群交给分帧拆除队列：不在爆炸同一帧删除上千实体。</summary>
	private void DetachSigil(ulong sid, Vector? center)
	{
		_detonateAt.Remove(sid);
		_nextSecond.Remove(sid);
		_center.Remove(sid);
		_castAt.Remove(sid);
		_dome.Remove(sid, out SigilDome? dome);
		_sky.Remove(sid, out SkySigilField? sky);

		List<IBeamGroup> groups = new List<IBeamGroup>();
		if (dome != null)
		{
			groups.Add(dome);
		}
		if (sky != null)
		{
			groups.Add(sky);
		}
		if (center != null)
		{
			_fade.Add(groups, center);
			return;
		}
		foreach (IBeamGroup group in groups)
		{
			group.Remove();
		}
	}

	private void ClearPending(ulong sid)
	{
		_detonateAt.Remove(sid);
		_nextSecond.Remove(sid);
		_center.Remove(sid);
		_castAt.Remove(sid);
		if (_dome.Remove(sid, out SigilDome? dome))
		{
			dome.Remove();
		}
		if (_sky.Remove(sid, out SkySigilField? sky))
		{
			sky.Remove();
		}
	}

	private static CCSPlayerController? FindBySteamId(ulong steamId)
	{
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && p.SteamID == steamId);
	}
}
