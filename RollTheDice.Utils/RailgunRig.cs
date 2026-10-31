#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Configs;

namespace RollTheDice.Utils;

/// <summary>
/// 「光之剑：超新星」的视觉装置：用 CBeam 拼出的**电磁炮构造体** + 白蓝色光束（Supernova dice 专用）。
///
/// <para>为什么这么做：CBeam 是 sprite 面片，单根没有体积感（旧版三层同轴 = 一片平面光）。
/// 本装置把 ≈86 根梁按"炮"的三维结构摆开（机匣/炮管方框 + 箍环 + 双导轨 + 炮口环 + 线圈 + 握把枪托），
/// 再配沿轴推进的流光环与脉冲 → 观感是一门立体的电磁轨道炮（参考《蔚蓝档案》爱丽丝的「光之剑：超新星」）。</para>
///
/// <para>零素材依赖：只用 CBeam + <see cref="ParticlePaths"/> 里的基础游戏粒子；
/// 拆除走 <see cref="SigilTeardown"/> 分帧队列（与坠落天空同款），不在结束帧批量删。</para>
///
/// <para>生命周期：<see cref="Stage.Charge"/>（炮体逐段亮起组装 / 导轨张开）→ <see cref="BeginFire"/>（炮口爆发）
/// → <see cref="Stage.Fire"/>（光束 + 流光环 + 脉冲）→ <see cref="EndFire"/> → <see cref="Stage.Fade"/>（0.2s 收细）
/// → <see cref="Update"/> 返回 false（交给分帧拆除队列）。</para>
/// </summary>
public sealed class RailgunRig : IBeamGroup
{
	public enum Stage
	{
		Charge,
		Fire,
		Fade,
		Done
	}

	/// <summary>局部坐标线段（x=右, y=前, z=上；炮尾在 y=0）。</summary>
	private sealed class Part
	{
		public float X1;
		public float Y1;
		public float Z1;
		public float X2;
		public float Y2;
		public float Z2;
		public float Width;
		public Color Color;
		/// <summary>组装进度（0~1）：充能推进到这个进度才创建。</summary>
		public float Reveal;
		/// <summary>导轨：充能时向外张开（开阖机关）。</summary>
		public bool Rail;
		/// <summary>能量件：呼吸加宽。</summary>
		public bool Pulse;
		public CBeam? Beam;
		public float BornAt = -1f;
	}

	// ── 配色（爱丽丝：白甲 + 青蓝光） ──
	private static readonly Color ArmorColor = Color.FromArgb(255, 232, 242, 255);
	private static readonly Color ArmorDimColor = Color.FromArgb(255, 178, 200, 224);
	private static readonly Color RailColor = Color.FromArgb(255, 127, 212, 255);
	private static readonly Color ChannelColor = Color.FromArgb(255, 226, 246, 255);
	private static readonly Color CoilColor = Color.FromArgb(255, 120, 220, 255);
	private static readonly Color BoreColor = Color.FromArgb(255, 42, 60, 85);
	private static readonly Color BatteryColor = Color.FromArgb(255, 255, 178, 77);
	private static readonly Color CableColor = Color.FromArgb(255, 122, 134, 150);
	private static readonly Color BeamCoreColor = Color.FromArgb(255, 255, 255, 255);
	private static readonly Color BeamMainColor = Color.FromArgb(255, 122, 214, 255);
	private static readonly Color BeamGlowColor = Color.FromArgb(255, 168, 224, 255);
	private static readonly Color RingColor = Color.FromArgb(255, 214, 242, 255);
	private static readonly Color PulseColor = Color.FromArgb(255, 255, 255, 255);

	// ── 炮体尺寸（缩放前） ──
	private const float BarrelHalf = 11f;
	private const float ReceiverHalf = 17f;
	private const float BarrelStart = 60f;
	private const float MuzzleY = 190f;
	private const float RailHalfZ = 6f;
	private const float RailOpenZ = 11f;
	private const float CoilY = 170f;
	private const float CoilRadius = 19f;
	private const float ScopeZ = 24f;

	private const float FadeSeconds = 0.2f;
	private const float BornPulseSeconds = 0.22f;
	private const float PulseTrailHalf = 42f;
	private const int RingSegments = 10;

	private readonly SupernovaConfig _cfg;
	private readonly float _startTime;
	private readonly float _scale;
	private readonly int _ringCount;
	/// <summary>false = 只出光束（关掉炮体构造体，留作性能/观感退路）。</summary>
	private readonly bool _gunParts;
	private readonly List<Part> _parts = new List<Part>();
	private readonly CBeam?[] _rings;
	private readonly CBeam?[] _pulses = new CBeam?[2];
	private readonly float[] _pulseStart = new float[2] { -999f, -999f };
	private CBeam? _core;
	private CBeam? _main;
	private CBeam? _glow;
	private CBeam? _sweep;

	private Stage _stage = Stage.Charge;
	private float _fireAt = -1f;
	private float _fadeAt = -1f;
	private float _pulseNext;
	private int _pulseSlot;
	private Vector _chest = null!;
	private Vector _forward = null!;
	private bool _hasFrame;

	public RailgunRig(SupernovaConfig cfg, Vector chest, Vector forward, float now)
	{
		_cfg = cfg;
		_startTime = now;
		_scale = Math.Clamp(cfg.GunScale, 0.4f, 3f);
		_gunParts = cfg.GunEnabled;
		_ringCount = Math.Clamp(cfg.RingCount, 0, 12);
		_rings = new CBeam?[_ringCount * RingSegments];
		BuildParts();
		Update(chest, forward, now);
	}

	public Stage CurrentStage => _stage;

	/// <summary>当前还活着的 CBeam 根数（调试回显）。</summary>
	public int BeamCount
	{
		get
		{
			int n = 0;
			if (BeamFx.IsAlive(_core))
			{
				n++;
			}
			if (BeamFx.IsAlive(_main))
			{
				n++;
			}
			if (BeamFx.IsAlive(_glow))
			{
				n++;
			}
			if (BeamFx.IsAlive(_sweep))
			{
				n++;
			}
			for (int i = 0; i < _rings.Length; i++)
			{
				if (BeamFx.IsAlive(_rings[i]))
				{
					n++;
				}
			}
			for (int i = 0; i < _pulses.Length; i++)
			{
				if (BeamFx.IsAlive(_pulses[i]))
				{
					n++;
				}
			}
			foreach (Part p in _parts)
			{
				if (BeamFx.IsAlive(p.Beam))
				{
					n++;
				}
			}
			return n;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (BeamFx.IsAlive(_core) || BeamFx.IsAlive(_main) || BeamFx.IsAlive(_glow) || BeamFx.IsAlive(_sweep))
			{
				return false;
			}
			for (int i = 0; i < _rings.Length; i++)
			{
				if (BeamFx.IsAlive(_rings[i]))
				{
					return false;
				}
			}
			for (int i = 0; i < _pulses.Length; i++)
			{
				if (BeamFx.IsAlive(_pulses[i]))
				{
					return false;
				}
			}
			foreach (Part p in _parts)
			{
				if (BeamFx.IsAlive(p.Beam))
				{
					return false;
				}
			}
			return true;
		}
	}

	/// <summary>充能结束 → 进入发射：点亮光束三层 / 流光环 / 脉冲，炮口爆发。</summary>
	public void BeginFire(float now)
	{
		if (_stage != Stage.Charge)
		{
			return;
		}
		_stage = Stage.Fire;
		_fireAt = now;
		_pulseNext = now;
		float mainWidth = MathF.Max(_cfg.BeamWidthOuter, 2f);
		float coreWidth = Math.Clamp(_cfg.BeamWidthInner, 1f, mainWidth);
		float glowWidth = mainWidth * Math.Clamp(_cfg.GlowWidthScale, 1f, 6f);
		// 三层光带：白芯 / 青主层（光晕）/ 淡蓝辉光；各层纹理滚动速度不同 = 光的奔流
		_glow = BeamFx.CreateStyled(_chest, _chest, BeamGlowColor, glowWidth, 1.6f, 0, 140f);
		_main = BeamFx.CreateStyled(_chest, _chest, BeamMainColor, mainWidth, 1f, 0, 190f);
		_core = BeamFx.CreateStyled(_chest, _chest, BeamCoreColor, coreWidth, 0f, 0, 240f);
		for (int i = 0; i < _rings.Length; i++)
		{
			_rings[i] = BeamFx.Create(RingColor, 3.4f * _scale);
		}
		if (_cfg.PulseInterval > 0.05f)
		{
			for (int i = 0; i < _pulses.Length; i++)
			{
				_pulses[i] = BeamFx.Create(PulseColor, 13f * _scale);
			}
		}
		// 炮口爆发（基础游戏粒子，已随 ParticlePaths 预缓存）
		Effects.Play(MuzzlePoint(), _cfg.MuzzleParticle, 0.35f);
	}

	/// <summary>发射结束 / 中断 → 0.2s 收细淡出（由 <see cref="Update"/> 收尾）。充能中被打断也走这里。</summary>
	public void EndFire(float now)
	{
		if (_stage == Stage.Charge || _stage == Stage.Fire)
		{
			_stage = Stage.Fade;
			_fadeAt = now;
		}
	}

	/// <summary>
	/// 每 tick 推进：变换炮体 / 推进组装 / 摆光束与流光环。返回 false = 全部结束（可转 <see cref="SigilTeardown"/>）。
	/// <paramref name="chest"/>/<paramref name="forward"/> 传 null 时沿用上次的帧（淡出期玩家已失效也能收尾）。
	/// </summary>
	public bool Update(Vector? chest, Vector? forward, float now)
	{
		if (chest != null && forward != null)
		{
			_chest = chest;
			_forward = forward;
			_hasFrame = true;
		}
		if (!_hasFrame)
		{
			return false;
		}
		ComputeFrame(out Vector o, out Vector f, out Vector r, out Vector u);

		if (_stage == Stage.Fade)
		{
			float t = Math.Clamp((now - _fadeAt) / FadeSeconds, 0f, 1f);
			ApplyWidths(1f - t);
			if (t >= 1f)
			{
				_stage = Stage.Done;
				return false;
			}
			return true;
		}

		float charge = Math.Clamp(_cfg.ChargeSeconds, 0f, 5f);
		float cp = (charge <= 0.001f) ? 1f : Math.Clamp((now - _startTime) / charge, 0f, 1f);
		bool firing = _stage != Stage.Charge;
		float reveal = firing ? 1f : cp;
		// 开阖机关：充能中段导轨张到最开，发射瞬间合拢
		float open = firing ? 0f : MathF.Sin(cp * MathF.PI);

		if (_gunParts)
		{
			EnsureParts(reveal, now);
			UpdateParts(o, f, r, u, open, now);
		}
		if (firing)
		{
			UpdateBeam(o, f, r, u, now);
		}
		else if (_gunParts)
		{
			UpdateSweep(o, f, r, u, cp);
		}
		return true;
	}
	// ── 炮体几何 ──

	private void BuildParts()
	{
		float s = _scale;
		float hb = BarrelHalf * s;
		float hr = ReceiverHalf * s;
		float y0 = BarrelStart * s;
		float y1 = MuzzleY * s;

		// 机匣：4 长棱 + 2 箍环 + 4 斜撑
		for (int i = 0; i < 4; i++)
		{
			float x = ((i & 1) == 0) ? -hr : hr;
			float z = ((i & 2) == 0) ? -hr : hr;
			AddPart(x, 0f, z, x, y0, z, 3.5f * s, ArmorColor, 0f);
		}
		AddHoop(0f, hr, 6, 3.2f * s, ArmorColor, 0.05f, 0f, false);
		AddHoop(30f * s, hr, 6, 3.2f * s, ArmorColor, 0.1f, 0f, false);
		AddPart(-hr, 6f * s, -hr, -hr, 34f * s, hr, 2.2f * s, ArmorDimColor, 0.15f);
		AddPart(-hr, 6f * s, hr, -hr, 34f * s, -hr, 2.2f * s, ArmorDimColor, 0.15f);
		AddPart(hr, 6f * s, -hr, hr, 34f * s, hr, 2.2f * s, ArmorDimColor, 0.15f);
		AddPart(hr, 6f * s, hr, hr, 34f * s, -hr, 2.2f * s, ArmorDimColor, 0.15f);

		// 炮管：4 长棱 + 3 箍环
		for (int i = 0; i < 4; i++)
		{
			float x = ((i & 1) == 0) ? -hb : hb;
			float z = ((i & 2) == 0) ? -hb : hb;
			AddPart(x, y0, z, x, y1, z, 3.5f * s, ArmorColor, 0.2f);
		}
		AddHoop(85f * s, hb, 6, 3.2f * s, ArmorColor, 0.3f, 0f, false);
		AddHoop(125f * s, hb, 6, 3.2f * s, ArmorColor, 0.4f, 0f, false);
		AddHoop(165f * s, hb, 6, 3.2f * s, ArmorColor, 0.5f, 0f, false);

		// 双导轨（充能时张开）+ 中间能量通道
		AddRail(-RailHalfZ * s, 70f * s, 185f * s, 5.5f * s, RailColor, 0.45f);
		AddRail(RailHalfZ * s, 70f * s, 185f * s, 5.5f * s, RailColor, 0.45f);
		AddPart(0f, 70f * s, 0f, 0f, 185f * s, 0f, 3.5f * s, ChannelColor, 0.55f, false, true);

		// 炮口：外环 + 内膛暗环 + 后方线圈环
		AddHoop(y1, 15f * s, 8, 4f * s, ArmorColor, 0.6f, 0f, false);
		AddHoop(y1 + 1f * s, 8f * s, 8, 3.5f * s, BoreColor, 0.6f, 0f, false);
		AddHoop(CoilY * s, CoilRadius * s, 8, 4f * s, CoilColor, 0.58f, 0f, true);

		// 顶部导轨 + 瞄具小环
		AddPart(0f, 10f * s, ScopeZ * s, 0f, 70f * s, ScopeZ * s, 2.5f * s, ArmorColor, 0.7f);
		AddHoop(25f * s, 5f * s, 4, 2f * s, ArmorColor, 0.75f, ScopeZ * s, false);
		AddHoop(55f * s, 5f * s, 4, 2f * s, ArmorColor, 0.75f, ScopeZ * s, false);

		// 握把 / 枪托
		AddPart(-2f * s, 40f * s, -hr, -2f * s, 22f * s, -32f * s, 5f * s, ArmorDimColor, 0.8f);
		AddPart(2f * s, 40f * s, -hr, 2f * s, 22f * s, -32f * s, 5f * s, ArmorDimColor, 0.8f);
		AddPart(0f, 0f, 14f * s, 0f, -38f * s, 8f * s, 6f * s, ArmorDimColor, 0.85f);
		AddPart(0f, 0f, -2f * s, 0f, -30f * s, -10f * s, 4f * s, ArmorDimColor, 0.85f);

		// 橙色能量电池（呼吸）+ 缆线
		AddPart(hr, 45f * s, 0f, hr, 55f * s, 0f, 4.5f * s, BatteryColor, 0.9f, false, true);
		AddPart(-hr, 45f * s, 0f, -hr, 55f * s, 0f, 4.5f * s, BatteryColor, 0.9f, false, true);
		AddPart(hr, 20f * s, 10f * s, hb, 100f * s, 8f * s, 1.8f * s, CableColor, 0.95f);
		AddPart(-hr, 20f * s, 10f * s, -hb, 100f * s, 8f * s, 1.8f * s, CableColor, 0.95f);
	}

	private void AddPart(float x1, float y1, float z1, float x2, float y2, float z2, float width, Color color, float reveal, bool rail = false, bool pulse = false)
	{
		_parts.Add(new Part
		{
			X1 = x1,
			Y1 = y1,
			Z1 = z1,
			X2 = x2,
			Y2 = y2,
			Z2 = z2,
			Width = width,
			Color = color,
			Reveal = reveal,
			Rail = rail,
			Pulse = pulse
		});
	}

	private void AddRail(float z, float y1, float y2, float width, Color color, float reveal)
	{
		AddPart(0f, y1, z, 0f, y2, z, width, color, reveal, true, false);
	}

	/// <summary>在 x-z 平面加一圈箍环（<paramref name="zc"/> 是环心高度偏移）。</summary>
	private void AddHoop(float y, float radius, int segments, float width, Color color, float reveal, float zc, bool pulse)
	{
		for (int i = 0; i < segments; i++)
		{
			float a1 = i * 2f * MathF.PI / segments;
			float a2 = (i + 1) * 2f * MathF.PI / segments;
			AddPart(
				MathF.Cos(a1) * radius, y, zc + MathF.Sin(a1) * radius,
				MathF.Cos(a2) * radius, y, zc + MathF.Sin(a2) * radius,
				width, color, reveal, false, pulse);
		}
	}

	// ── 每 tick ──

	private void ComputeFrame(out Vector o, out Vector f, out Vector r, out Vector u)
	{
		f = Vectors.Normalize(_forward);
		if (f.X == 0f && f.Y == 0f && f.Z == 0f)
		{
			f = new Vector(1f, 0f, 0f);
		}
		r = Vectors.Normalize(Vectors.Cross(f, new Vector(0f, 0f, 1f)));
		if (r.X == 0f && r.Y == 0f && r.Z == 0f)
		{
			r = new Vector(1f, 0f, 0f);
		}
		u = Vectors.Normalize(Vectors.Cross(r, f));
		// 炮基座 = 胸口 + 右×side − 上×drop + 前×forward
		o = new Vector(
			_chest.X + r.X * _cfg.GunSide - u.X * _cfg.GunDrop + f.X * _cfg.GunForward,
			_chest.Y + r.Y * _cfg.GunSide - u.Y * _cfg.GunDrop + f.Y * _cfg.GunForward,
			_chest.Z + r.Z * _cfg.GunSide - u.Z * _cfg.GunDrop + f.Z * _cfg.GunForward);
	}

	private static Vector World(Vector o, Vector f, Vector r, Vector u, float x, float y, float z)
	{
		return new Vector(
			o.X + r.X * x + f.X * y + u.X * z,
			o.Y + r.Y * x + f.Y * y + u.Y * z,
			o.Z + r.Z * x + f.Z * y + u.Z * z);
	}

	private Vector MuzzlePoint()
	{
		ComputeFrame(out Vector o, out Vector f, out Vector r, out Vector u);
		return World(o, f, r, u, 0f, MuzzleY * _scale, 0f);
	}

	/// <summary>炮体逐段亮起组装（≈3 束/tick，避免同帧创建上百实体）。</summary>
	private void EnsureParts(float reveal, float now)
	{
		foreach (Part p in _parts)
		{
			if (p.Beam == null && reveal >= p.Reveal)
			{
				p.Beam = BeamFx.Create(p.Color, p.Width);
				p.BornAt = now;
			}
		}
	}

	private void UpdateParts(Vector o, Vector f, Vector r, Vector u, float open, float now)
	{
		float railZ = (RailHalfZ + (RailOpenZ - RailHalfZ) * open) * _scale;
		foreach (Part p in _parts)
		{
			if (p.Beam == null)
			{
				continue;
			}
			float z1 = p.Rail ? MathF.Sign(p.Z1) * railZ : p.Z1;
			float z2 = p.Rail ? MathF.Sign(p.Z2) * railZ : p.Z2;
			BeamFx.Move(p.Beam, World(o, f, r, u, p.X1, p.Y1, z1), World(o, f, r, u, p.X2, p.Y2, z2));
			if (p.Pulse)
			{
				BeamFx.SetWidth(p.Beam, MathF.Max(p.Width * (1f + 0.18f * MathF.Sin(now * 9f)), 0.2f));
			}
			if (p.BornAt >= 0f)
			{
				float age = now - p.BornAt;
				if (age >= BornPulseSeconds)
				{
					p.BornAt = -1f;
					BeamFx.SetWidth(p.Beam, p.Width);
				}
				else
				{
					float pulse = 1f + 0.9f * MathF.Sin(age / BornPulseSeconds * MathF.PI);
					BeamFx.SetWidth(p.Beam, MathF.Max(p.Width * pulse, 0.2f));
				}
			}
		}
	}

	/// <summary>充能期：一道亮段沿能量通道从后往前扫（充能感）。</summary>
	private void UpdateSweep(Vector o, Vector f, Vector r, Vector u, float cp)
	{
		if (_sweep == null)
		{
			_sweep = BeamFx.Create(ChannelColor, 5f * _scale);
			if (_sweep == null)
			{
				return;
			}
		}
		float y0 = 70f * _scale;
		float y1 = 185f * _scale;
		float y = y0 + (y1 - y0) * cp;
		float half = 16f * _scale;
		BeamFx.Move(_sweep,
			World(o, f, r, u, 0f, MathF.Max(y - half, y0), 0f),
			World(o, f, r, u, 0f, MathF.Min(y + half, y1), 0f));
		BeamFx.SetWidth(_sweep, MathF.Max((3f + 4f * MathF.Sin(cp * MathF.PI)) * _scale, 0.8f));
	}

	/// <summary>发射期：光束三层 + 流光环（从炮口向末端循环推进）+ 脉冲（电磁炮连射感）。</summary>
	private void UpdateBeam(Vector o, Vector f, Vector r, Vector u, float now)
	{
		Vector muzzle = World(o, f, r, u, 0f, MuzzleY * _scale, 0f);
		float range = MathF.Max(_cfg.Range, 100f);
		Vector end = new Vector(_chest.X + f.X * range, _chest.Y + f.Y * range, _chest.Z + f.Z * range);
		BeamFx.Move(_core, muzzle, end);
		BeamFx.Move(_main, muzzle, end);
		BeamFx.Move(_glow, muzzle, end);

		float beamLen = MathF.Max(Vectors.GetDistance(muzzle, end), 1f);
		float age = now - _fireAt;
		float speed = MathF.Max(_cfg.RingSpeed, 0.05f);
		float radiusBase = MathF.Max(_cfg.RingRadius, 6f);
		for (int i = 0; i < _ringCount; i++)
		{
			float p = (age * speed + (float)i / MathF.Max(_ringCount, 1)) % 1f;
			float dist = p * beamLen;
			float radius = radiusBase * (1f - 0.3f * p);
			float cx = muzzle.X + f.X * dist;
			float cy = muzzle.Y + f.Y * dist;
			float cz = muzzle.Z + f.Z * dist;
			for (int s = 0; s < RingSegments; s++)
			{
				CBeam? beam = _rings[i * RingSegments + s];
				if (beam == null)
				{
					continue;
				}
				float a1 = s * 2f * MathF.PI / RingSegments;
				float a2 = (s + 1) * 2f * MathF.PI / RingSegments;
				BeamFx.Move(beam, RingPoint(cx, cy, cz, r, u, a1, radius), RingPoint(cx, cy, cz, r, u, a2, radius));
			}
		}

		float interval = _cfg.PulseInterval;
		if (interval <= 0.05f)
		{
			return;
		}
		if (now >= _pulseNext)
		{
			_pulseNext = now + interval;
			_pulseSlot = 1 - _pulseSlot;
			_pulseStart[_pulseSlot] = now;
			Effects.Play(muzzle, ParticlePaths.ElectricGlow, 0.18f);
		}
		float trailSeconds = MathF.Max(interval, 0.1f);
		for (int i = 0; i < _pulses.Length; i++)
		{
			CBeam? pulse = _pulses[i];
			if (pulse == null)
			{
				continue;
			}
			float t = (now - _pulseStart[i]) / trailSeconds;
			if (t < 0f || t > 1f)
			{
				BeamFx.Move(pulse, muzzle, muzzle);
				continue;
			}
			float d = t * beamLen;
			float a = MathF.Max(d - PulseTrailHalf, 0f);
			float b = MathF.Min(d + PulseTrailHalf, beamLen);
			BeamFx.Move(pulse,
				new Vector(muzzle.X + f.X * a, muzzle.Y + f.Y * a, muzzle.Z + f.Z * a),
				new Vector(muzzle.X + f.X * b, muzzle.Y + f.Y * b, muzzle.Z + f.Z * b));
		}
	}

	private static Vector RingPoint(float cx, float cy, float cz, Vector r, Vector u, float angle, float radius)
	{
		float c = MathF.Cos(angle) * radius;
		float s = MathF.Sin(angle) * radius;
		return new Vector(cx + r.X * c + u.X * s, cy + r.Y * c + u.Y * s, cz + r.Z * c + u.Z * s);
	}

	/// <summary>淡出：炮体 / 光束 / 环 / 脉冲一起收细。</summary>
	private void ApplyWidths(float scale)
	{
		float s = Math.Clamp(scale, 0f, 1f);
		foreach (Part p in _parts)
		{
			BeamFx.SetWidth(p.Beam, MathF.Max(p.Width * s, 0.2f));
		}
		BeamFx.SetWidth(_glow, MathF.Max(MathF.Max(_cfg.BeamWidthOuter, 2f) * Math.Clamp(_cfg.GlowWidthScale, 1f, 6f) * s, 0.2f));
		BeamFx.SetWidth(_main, MathF.Max(MathF.Max(_cfg.BeamWidthOuter, 2f) * s, 0.2f));
		BeamFx.SetWidth(_core, MathF.Max(MathF.Max(_cfg.BeamWidthInner, 1f) * s, 0.2f));
		for (int i = 0; i < _rings.Length; i++)
		{
			BeamFx.SetWidth(_rings[i], MathF.Max(3.4f * _scale * s, 0.2f));
		}
		for (int i = 0; i < _pulses.Length; i++)
		{
			BeamFx.SetWidth(_pulses[i], MathF.Max(13f * _scale * s, 0.2f));
		}
	}

	// ── 拆除 ──

	public void Remove()
	{
		BeamFx.Kill(_glow);
		_glow = null;
		BeamFx.Kill(_main);
		_main = null;
		BeamFx.Kill(_core);
		_core = null;
		BeamFx.Kill(_sweep);
		_sweep = null;
		for (int i = 0; i < _rings.Length; i++)
		{
			BeamFx.Kill(_rings[i]);
			_rings[i] = null;
		}
		for (int i = 0; i < _pulses.Length; i++)
		{
			BeamFx.Kill(_pulses[i]);
			_pulses[i] = null;
		}
		foreach (Part p in _parts)
		{
			BeamFx.Kill(p.Beam);
			p.Beam = null;
		}
		_stage = Stage.Done;
	}

	/// <summary>分帧拆除：先收光束/环/脉冲，再拆炮体。</summary>
	public int RemoveChunk(int budget)
	{
		int cap = Math.Max(budget, 0);
		if (cap == 0)
		{
			return 0;
		}
		int removed = 0;
		if (_glow != null)
		{
			BeamFx.Kill(_glow);
			_glow = null;
			removed++;
		}
		if (_main != null)
		{
			BeamFx.Kill(_main);
			_main = null;
			removed++;
		}
		if (_core != null)
		{
			BeamFx.Kill(_core);
			_core = null;
			removed++;
		}
		if (_sweep != null)
		{
			BeamFx.Kill(_sweep);
			_sweep = null;
			removed++;
		}
		for (int i = 0; i < _rings.Length && removed < cap; i++)
		{
			if (_rings[i] != null)
			{
				BeamFx.Kill(_rings[i]);
				_rings[i] = null;
				removed++;
			}
		}
		for (int i = 0; i < _pulses.Length && removed < cap; i++)
		{
			if (_pulses[i] != null)
			{
				BeamFx.Kill(_pulses[i]);
				_pulses[i] = null;
				removed++;
			}
		}
		foreach (Part p in _parts)
		{
			if (removed >= cap)
			{
				break;
			}
			if (p.Beam != null)
			{
				BeamFx.Kill(p.Beam);
				p.Beam = null;
				removed++;
			}
		}
		if (IsEmpty)
		{
			_stage = Stage.Done;
		}
		return removed;
	}
}
