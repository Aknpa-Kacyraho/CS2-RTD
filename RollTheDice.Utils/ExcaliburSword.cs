#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Configs;

namespace RollTheDice.Utils;

/// <summary>
/// 「誓约胜利之剑」的视觉装置（Excalibur dice 专用）。
///
/// <para>蓄力：12 段光丝从周围球壳沿弧线飞入、逐段嵌合成竖直光剑（落位闪光），
/// 剑长随进度伸展；汇聚光丝被持续吸进剑体；脚下 Sun 风格法阵从 420 收拢到 90；
/// 生长阶段白芯与外辉光从拼好的剑里点亮。</para>
///
/// <para>释放：0.12s 挥落（剑轴从世界 up 转向准星 forward）→ 4 层水平光炮
/// 宽度 ease-out 扩张 → 保持 → 收缩淡出。光柱位置在释放瞬间锁定（穿墙、不跟随）。</para>
///
/// <para>拆除：法阵/光丝在 Release 时、剑在挥落结束时分别转入 <see cref="TakeRetired"/> 交给
/// <see cref="SigilTeardown"/> 分帧队列；光柱由调用方在结束后直接 Remove（仅 4 根）。</para>
/// </summary>
public sealed class ExcaliburSword : IBeamGroup
{
	private enum Stage
	{
		Charge,
		Swing,
		Done
	}

	private sealed class Segment
	{
		public CBeam? Beam;
		public Vector Scatter = null!;
		public Vector Control = null!;
		public int Index;
		public float Delay;
		public float FlySeconds;
		public bool Landed;
		public bool Flashed;
		public float LandedAt;
	}

	/// <summary>把一组裸 CBeam 包成 IBeamGroup，交给分帧拆除队列。</summary>
	private sealed class BeamPack : IBeamGroup
	{
		private readonly List<CBeam?> _beams = new List<CBeam?>();

		public BeamPack(IEnumerable<CBeam?> beams)
		{
			foreach (CBeam? b in beams)
			{
				if (b != null)
				{
					_beams.Add(b);
				}
			}
		}

		public bool IsEmpty => _beams.Count == 0;

		public int RemoveChunk(int budget)
		{
			int removed = 0;
			int cap = Math.Max(budget, 1);
			while (_beams.Count > 0 && removed < cap)
			{
				CBeam? b = _beams[_beams.Count - 1];
				_beams.RemoveAt(_beams.Count - 1);
				BeamFx.Kill(b);
				removed++;
			}
			return removed;
		}

		public void Remove()
		{
			foreach (CBeam? b in _beams)
			{
				BeamFx.Kill(b);
			}
			_beams.Clear();
		}
	}

	private static readonly Color SegmentColor = Color.FromArgb(255, 255, 226, 130);
	private static readonly Color CoreColor = Color.FromArgb(255, 255, 252, 235);
	private static readonly Color GlowColor = Color.FromArgb(255, 255, 232, 150);
	private static readonly Color StreakColor = Color.FromArgb(255, 255, 214, 100);
	private static readonly Color SigilCore = Color.FromArgb(255, 255, 248, 220);
	private static readonly Color SigilMid = Color.FromArgb(255, 255, 205, 40);
	private static readonly Color SigilHalo = Color.FromArgb(255, 255, 170, 20);
	private static readonly Color Sigil2Core = Color.FromArgb(255, 255, 252, 235);
	private static readonly Color Sigil2Mid = Color.FromArgb(255, 255, 228, 150);
	private static readonly Color Sigil2Halo = Color.FromArgb(255, 240, 190, 90);

	private const float HeadOffset = 40f;
	private const float ChestOffset = 50f;
	private const float SegmentOverlap = 1.3f;
	private const float SwingSeconds = 0.12f;
	private const float GrowStart = 0.72f;
	private const float SigilPulseInterval = 0.12f;
	/// <summary>剑基座相对玩家的右 / 前偏移（第一人称可见）。</summary>
	private const float SwordSideOffset = 46f;
	private const float SwordForwardOffset = 58f;

	private readonly ExcaliburConfig _cfg;
	private readonly float _startTime;
	private readonly List<Segment> _segments;
	private readonly SigilShapeGroup _sigil;
	private readonly SigilShapeGroup _sigil2;
	private readonly CBeam?[] _streaks;
	private readonly Vector[] _streakByDir;
	private readonly float[] _streakPhase;
	private readonly List<IBeamGroup> _retired = new List<IBeamGroup>();
	private readonly List<IBeamGroup> _empty = new List<IBeamGroup>();

	private Vector _origin;
	private CBeam? _core;
	private CBeam? _glow;
	// ★ 实体剑（2026-09-22 起）：模型代替 CBeam 剑身，拼凑到 sword_model_spawn_progress 后出现，之后每 tick 跟着剑轴走。
	private CDynamicProp? _model;
	private bool _modelTried;
	private CBeam? _topGlow;
	private CBeam? _topCore;
	private readonly CBeam?[] _crossguard = new CBeam?[2];
	private readonly CBeam?[] _tipCross = new CBeam?[2];
	private readonly CBeam?[] _runes = new CBeam?[5];
	private Vector _forward = null!;
	private float _releaseTime;
	private float _chargeLength = 100f;
	private float _streakAcc;
	private float _lastUpdate;
	private float _nextSigilPulse;
	private Stage _stage = Stage.Charge;

	public ExcaliburSword(ExcaliburConfig cfg, Vector origin, float now, int seed)
	{
		_cfg = cfg;
		_startTime = now;
		_lastUpdate = now;
		_origin = new Vector(origin.X, origin.Y, origin.Z);
		Random rng = new Random(seed);

		// 拼凑段：散落在周围球壳，各自带起飞延迟
		int count = Math.Clamp(cfg.SegmentCount, 4, 32);
		_segments = new List<Segment>(count);
		float rMin = 250f;
		float rMax = MathF.Max(cfg.StreakRadius, rMin + 40f);
		float spread = MathF.Max(cfg.ChargeSeconds * 0.72f, 1f);
		float slot0Gap = MathF.Max(cfg.SwordChargeLength, 20f) / count;
		for (int i = 0; i < count; i++)
		{
			float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
			float rad = rMin + (float)rng.NextDouble() * (rMax - rMin);
			float h = -50f + (float)rng.NextDouble() * 300f;
			Vector scatter = new Vector(origin.X + MathF.Cos(ang) * rad, origin.Y + MathF.Sin(ang) * rad, origin.Z + h);
			float slot0 = origin.Z + HeadOffset + (i + 0.5f) * slot0Gap;
			Vector mid = new Vector((scatter.X + origin.X) * 0.5f, (scatter.Y + origin.Y) * 0.5f, (scatter.Z + slot0) * 0.5f);
			float ox = (float)(rng.NextDouble() * 2.0 - 1.0);
			float oy = (float)(rng.NextDouble() * 2.0 - 1.0);
			float oz = (float)(rng.NextDouble() * 2.0 - 1.0);
			float ol = MathF.Max(MathF.Sqrt(ox * ox + oy * oy + oz * oz), 0.001f);
			float amp = 70f + (float)rng.NextDouble() * 120f;
			Vector control = new Vector(mid.X + ox / ol * amp, mid.Y + oy / ol * amp, mid.Z + oz / ol * amp);
			_segments.Add(new Segment
			{
				Beam = BeamFx.Create(SegmentColor, MathF.Max(cfg.SegmentWidth, 0.8f)),
				Scatter = scatter,
				Control = control,
				Index = i,
				Delay = (float)rng.NextDouble() * spread,
				FlySeconds = 0.45f + (float)rng.NextDouble() * 0.25f
			});
		}

		// 脚下 Sun 风格金色法阵（随进度收缩）
		_sigil = new SigilShapeGroup { Stride = 2 };
		SigilPalette palette = new SigilPalette(SigilCore, SigilMid, SigilHalo);
		SigilParams parameters = SigilParams.From(cfg.SigilDensity, 84, 42, 6, 2, 8, true, seed);
		_sigil.AddRange(SigilStyles.Build(SigilStyle.Sun, _origin, MathF.Max(cfg.SigilRadiusStart, 40f), parameters, palette, MathF.Max(cfg.SigilWidth, 0.8f), cfg.SigilSpin, 1f, true));
		_sigil.Update(_origin, now, 1f);

		// 第二层法阵（外层逆行）：与 Sun 双层叠加
		_sigil2 = new SigilShapeGroup { Stride = 2 };
		SigilPalette palette2 = new SigilPalette(Sigil2Core, Sigil2Mid, Sigil2Halo);
		_sigil2.AddRange(SigilStyles.Build(SigilStyles.Parse(cfg.Sigil2Style), _origin, MathF.Max(cfg.Sigil2RadiusStart, 40f), parameters, palette2, MathF.Max(cfg.SigilWidth, 0.8f), cfg.Sigil2Spin, 0.85f, true));
		_sigil2.Update(_origin, now, 1f);

		// 剑的装饰件：剑格十字 / 剑尖十字 / 剑身符文刻度（随剑轴每帧重摆）
		// ★ 换成实体剑后这些"线条假剑形"默认关掉（sword_model_ornaments），别糊在模型上。
		Color frameColor = Color.FromArgb(255, 255, 236, 170);
		if (string.IsNullOrWhiteSpace(cfg.SwordModel) || cfg.SwordModelOrnaments)
		{
			for (int i = 0; i < _crossguard.Length; i++)
			{
				_crossguard[i] = BeamFx.Create(frameColor, 5f);
			}
			for (int i = 0; i < _tipCross.Length; i++)
			{
				_tipCross[i] = BeamFx.Create(Color.FromArgb(255, 255, 250, 220), 4.5f);
			}
			for (int i = 0; i < _runes.Length; i++)
			{
				_runes[i] = BeamFx.Create(Color.FromArgb(255, 255, 244, 190), 2.6f);
			}
		}
		// 剑顶冲天光柱（引星之光）
		_topGlow = BeamFx.Create(Sigil2Halo, 1.2f);
		_topCore = BeamFx.Create(Color.FromArgb(255, 255, 252, 235), 0.8f);

		// 向心汇聚流（渐进创建）
		int streakCount = Math.Clamp(cfg.StreakCount, 0, 64);
		_streaks = new CBeam?[streakCount];
		_streakByDir = new Vector[streakCount];
		_streakPhase = new float[streakCount];
		for (int i = 0; i < streakCount; i++)
		{
			float a = (float)(rng.NextDouble() * Math.PI * 2.0);
			float elev = ((float)rng.NextDouble() * 2f - 1f) * 0.6f;
			float ce = MathF.Cos(elev);
			_streakByDir[i] = new Vector(MathF.Cos(a) * ce, MathF.Sin(a) * ce, MathF.Sin(elev));
			_streakPhase[i] = (float)rng.NextDouble();
		}

		// 立即摆一次位（段 / 装饰件 / 冲天光柱）：否则会先在 (0,0,0) 闪现一帧
		UpdateCharge(_origin, new Vector(1f, 0f, 0f), 0f, now);
	}

	/// <summary>当前剑长（随蓄力进度伸展）。</summary>
	public float SwordLength => _chargeLength;

	public bool Released => _stage != Stage.Charge;

	public bool Finished => _stage == Stage.Done;

	/// <summary>蓄力推进：拼凑 / 生长 / 法阵收拢 / 汇聚流（由调用方每 tick 调用；forward 用于剑基座定位）。</summary>
	public void UpdateCharge(Vector origin, Vector forward, float progress, float now)
	{
		if (_stage != Stage.Charge)
		{
			return;
		}
		_origin = origin;
		progress = Math.Clamp(progress, 0f, 1f);
		float ease = 1f - (1f - progress) * (1f - progress);
		float length = MathF.Max(_cfg.SwordChargeLength + (MathF.Max(_cfg.SwordLength, _cfg.SwordChargeLength) - _cfg.SwordChargeLength) * ease, 20f);
		_chargeLength = length;
		// 剑基座 = 玩家右前方（第一人称可见：竖直剑立在右手边）；挥落时朝准星劈下
		Vector basePos = SwordBase(origin, forward);
		float headZ = basePos.Z;
		float dt = Math.Clamp(now - _lastUpdate, 0f, 0.5f);
		_lastUpdate = now;

		// 法阵：整体收缩 + 节流脉冲加粗
		float sigilEnd = MathF.Max(_cfg.SigilRadiusEnd, 10f);
		float sigilStart = MathF.Max(_cfg.SigilRadiusStart, sigilEnd);
		float scale = (sigilStart + (sigilEnd - sigilStart) * ease) / sigilStart;
		_sigil.Update(origin, now, scale);
		// 第二层法阵：独立半径收缩（外层逆行）
		float sigil2End = MathF.Max(_cfg.Sigil2RadiusEnd, 10f);
		float sigil2Start = MathF.Max(_cfg.Sigil2RadiusStart, sigil2End);
		float scale2 = (sigil2Start + (sigil2End - sigil2Start) * ease) / sigil2Start;
		_sigil2.Update(origin, now, scale2);
		if (now >= _nextSigilPulse)
		{
			_nextSigilPulse = now + SigilPulseInterval;
			float w = 1f + 0.4f * (0.5f + 0.5f * MathF.Sin(now * 7f)) * (0.35f + 0.65f * progress);
			foreach (SigilShape shape in _sigil.Shapes)
			{
				shape.SetWidthScale(w);
			}
			foreach (SigilShape shape in _sigil2.Shapes)
			{
				shape.SetWidthScale(w * 0.85f);
			}
		}

		// 拼凑段：追着随剑长移动的槽位飞
		float elapsed = now - _startTime;
		float segGap = length / _segments.Count;
		float half = segGap * SegmentOverlap * 0.5f;
		foreach (Segment seg in _segments)
		{
			if (seg.Beam == null)
			{
				continue;
			}
			float slotZ = headZ + (seg.Index + 0.5f) * segGap;
			if (seg.Landed)
			{
				BeamFx.Move(seg.Beam, new Vector(basePos.X, basePos.Y, slotZ - half), new Vector(basePos.X, basePos.Y, slotZ + half));
				float age = now - seg.LandedAt;
				if (age < 0.28f)
				{
					float pulse = 1f + 0.9f * MathF.Sin(age / 0.28f * MathF.PI);
					BeamFx.SetWidth(seg.Beam, MathF.Max(_cfg.SegmentWidth * pulse, 0.8f));
				}
				continue;
			}
			float t = seg.FlySeconds > 0.001f ? Math.Clamp((elapsed - seg.Delay) / seg.FlySeconds, 0f, 1f) : 1f;
			float te = 1f - (1f - t) * (1f - t) * (1f - t);
			Vector slot = new Vector(basePos.X, basePos.Y, slotZ);
			Vector cur = Bezier(seg.Scatter, seg.Control, slot, te);
			Vector fly = new Vector(slot.X - cur.X, slot.Y - cur.Y, slot.Z - cur.Z);
			float fl = MathF.Max(MathF.Sqrt(fly.X * fly.X + fly.Y * fly.Y + fly.Z * fly.Z), 0.001f);
			BeamFx.Move(seg.Beam,
				new Vector(cur.X - fly.X / fl * half, cur.Y - fly.Y / fl * half, cur.Z - fly.Z / fl * half),
				new Vector(cur.X + fly.X / fl * half, cur.Y + fly.Y / fl * half, cur.Z + fly.Z / fl * half));
			if (t >= 0.999f)
			{
				seg.Landed = true;
				seg.LandedAt = now;
				if (!seg.Flashed)
				{
					seg.Flashed = true;
					Effects.Play(slot, ParticlePaths.GoldHaloFlare, 0.18f);
				}
			}
		}

		// 生长阶段：白芯 + 外辉光从拼好的剑里点亮（★ 有实体剑时不再需要这两层"光剑身"）
		if (progress >= GrowStart && _model == null)
		{
			float gt = Math.Clamp((progress - GrowStart) / (1f - GrowStart), 0f, 1f);
			if (_core == null)
			{
				_core = BeamFx.Create(CoreColor, 0.6f);
			}
			if (_glow == null)
			{
				_glow = BeamFx.Create(GlowColor, 0.6f);
			}
			Vector a = new Vector(basePos.X, basePos.Y, headZ);
			Vector b = new Vector(basePos.X, basePos.Y, headZ + length);
			BeamFx.Move(_core, a, b);
			BeamFx.Move(_glow, a, b);
			BeamFx.SetWidth(_core, MathF.Max(_cfg.SwordCoreWidth * gt, 0.6f));
			BeamFx.SetWidth(_glow, MathF.Max(_cfg.SwordGlowWidth * gt, 0.6f));
		}

		// 剑格 / 剑尖 / 符文刻度 / 剑顶冲天光柱：按当前剑轴与剑长重摆
		UpdateSwordOrnaments(basePos, length, new Vector(0f, 0f, 1f), progress);
		// 实体剑：拼凑到一定进度后出现，之后跟着竖直剑轴（挥落时改跟挥落方向）
		UpdateSwordModel(basePos, new Vector(0f, 0f, 1f), progress, length);

		// 汇聚流：从外围环带被吸向剑体，循环重生，数量随进度增多
		_streakAcc += (0.35f + 0.55f * progress) * dt;
		_streakAcc %= 1f;
		int active = _streaks.Length;
		if (active > 0)
		{
			Vector mid = new Vector(basePos.X, basePos.Y, headZ + length * 0.55f);
			float streakRadius = MathF.Max(_cfg.StreakRadius, 120f);
			for (int i = 0; i < active; i++)
			{
				if (_streaks[i] == null)
				{
					if (progress < (i + 1f) / active * 0.7f)
					{
						continue;
					}
					_streaks[i] = BeamFx.Create(StreakColor, 1.4f);
					if (_streaks[i] == null)
					{
						continue;
					}
				}
				float pp = (_streakAcc + _streakPhase[i]) % 1f;
				float distOut = streakRadius * (1f - pp);
				Vector dir = _streakByDir[i];
				BeamFx.Move(_streaks[i],
					new Vector(mid.X + dir.X * distOut, mid.Y + dir.Y * distOut, mid.Z + dir.Z * distOut),
					new Vector(mid.X + dir.X * 16f, mid.Y + dir.Y * 16f, mid.Z + dir.Z * 16f));
			}
		}
	}

	/// <summary>剑格 / 剑尖十字 / 符文刻度 / 剑顶冲天光柱：按当前剑轴与剑长重摆（蓄力与挥落共用）。</summary>
	private void UpdateSwordOrnaments(Vector basePos, float length, Vector dir, float progress)
	{
		Vector right = Vectors.Normalize(Vectors.Cross(dir, new Vector(0f, 0f, 1f)));
		if (right.X == 0f && right.Y == 0f && right.Z == 0f)
		{
			right = new Vector(1f, 0f, 0f);
		}
		Vector perp = Vectors.Normalize(Vectors.Cross(right, dir));
		// 剑格：剑轴底部 6% 处的十字（臂随进度张开）
		float arm = 34f + 26f * progress;
		Vector guard = new Vector(basePos.X + dir.X * length * 0.06f, basePos.Y + dir.Y * length * 0.06f, basePos.Z + dir.Z * length * 0.06f);
		BeamFx.Move(_crossguard[0],
			new Vector(guard.X - right.X * arm, guard.Y - right.Y * arm, guard.Z - right.Z * arm),
			new Vector(guard.X + right.X * arm, guard.Y + right.Y * arm, guard.Z + right.Z * arm));
		BeamFx.Move(_crossguard[1],
			new Vector(guard.X - perp.X * arm, guard.Y - perp.Y * arm, guard.Z - perp.Z * arm),
			new Vector(guard.X + perp.X * arm, guard.Y + perp.Y * arm, guard.Z + perp.Z * arm));
		// 剑格也随拼凑进度显形（别一开始就有完整骨架）
		float guardW = 0.6f + 4.4f * Math.Clamp((progress - 0.12f) / 0.55f, 0f, 1f);
		BeamFx.SetWidth(_crossguard[0], guardW);
		BeamFx.SetWidth(_crossguard[1], guardW);
		// 剑尖十字
		Vector tip = new Vector(basePos.X + dir.X * length, basePos.Y + dir.Y * length, basePos.Z + dir.Z * length);
		float tipArm = 30f + 44f * progress;
		BeamFx.Move(_tipCross[0],
			new Vector(tip.X - right.X * tipArm, tip.Y - right.Y * tipArm, tip.Z - right.Z * tipArm),
			new Vector(tip.X + right.X * tipArm, tip.Y + right.Y * tipArm, tip.Z + right.Z * tipArm));
		BeamFx.Move(_tipCross[1],
			new Vector(tip.X - perp.X * tipArm, tip.Y - perp.Y * tipArm, tip.Z - perp.Z * tipArm),
			new Vector(tip.X + perp.X * tipArm, tip.Y + perp.Y * tipArm, tip.Z + perp.Z * tipArm));
		float tipW = 0.6f + 3.9f * Math.Clamp((progress - 0.55f) / 0.45f, 0f, 1f);
		BeamFx.SetWidth(_tipCross[0], tipW);
		BeamFx.SetWidth(_tipCross[1], tipW);
		// 符文刻度：沿剑身 5 道短横线，随进度自下而上点亮
		for (int i = 0; i < _runes.Length; i++)
		{
			float at = 0.18f + 0.64f * i / (_runes.Length - 1);
			Vector p = new Vector(basePos.X + dir.X * length * at, basePos.Y + dir.Y * length * at, basePos.Z + dir.Z * length * at);
			float half = 9f;
			BeamFx.Move(_runes[i],
				new Vector(p.X - right.X * half, p.Y - right.Y * half, p.Z - right.Z * half),
				new Vector(p.X + right.X * half, p.Y + right.Y * half, p.Z + right.Z * half));
			float lit = Math.Clamp((progress - at * 0.9f) * 6f, 0f, 1f);
			BeamFx.SetWidth(_runes[i], 1.2f + 3.4f * lit);
		}
		// 剑顶冲天光柱（引星之光）：只在满蓄最后一段亮起——平时藏起来，别抢戏、别挡住剑的拼凑过程
		float skyT = Math.Clamp((progress - 0.9f) / 0.1f, 0f, 1f);
		if (skyT <= 0f)
		{
			BeamFx.Move(_topGlow, tip, tip);
			BeamFx.Move(_topCore, tip, tip);
			BeamFx.SetWidth(_topGlow, 0.5f);
			BeamFx.SetWidth(_topCore, 0.5f);
		}
		else
		{
			Vector sky = new Vector(tip.X, tip.Y, tip.Z + MathF.Max(_cfg.TopPillarHeight, 200f));
			BeamFx.Move(_topGlow, tip, sky);
			BeamFx.Move(_topCore, tip, sky);
			BeamFx.SetWidth(_topGlow, MathF.Max(_cfg.TopPillarWidth * skyT, 0.6f));
			BeamFx.SetWidth(_topCore, MathF.Max(_cfg.TopPillarWidth * 0.4f * skyT, 0.5f));
		}
	}

	/// <summary>
	/// 实体剑：到 <c>sword_model_spawn_progress</c> 才创建（懒建，避免早早挂一个不动的模型），
	/// 之后每 tick 把它的 forward（模型本地 +Y = 剑身方向）对准剑轴 <paramref name="dir"/>，再按配置翻转/滚转。
	/// 模型创建失败（路径不在包里等）时 <see cref="_model"/> 保持 null → 自动退回 CBeam 剑身。
	/// </summary>
	private void UpdateSwordModel(Vector basePos, Vector dir, float progress, float length)
	{
		if (string.IsNullOrWhiteSpace(_cfg.SwordModel))
		{
			return;
		}
		if (_model == null || !_model.IsValid)
		{
			// 只尝试建一次：失败（路径不在包里等）就不再每 tick 重试 → 自动退回 CBeam 剑身。
			if (_modelTried || progress < Math.Clamp(_cfg.SwordModelSpawnProgress, 0f, 1f))
			{
				return;
			}
			_modelTried = true;
			_model = Entities.CreatePropEntity(basePos, new QAngle(0f, 0f, 0f), _cfg.SwordModel, MathF.Max(_cfg.SwordModelScale, 0.1f));
			if (_model == null)
			{
				return;
			}
			try
			{
				// 纯视觉：不要碰撞。
				((CBaseModelEntity)_model).Collision.SolidType = (SolidType_t)0;
			}
			catch
			{
			}
			Effects.Play(new Vector(basePos.X, basePos.Y, basePos.Z + length * 0.5f), ParticlePaths.GoldHaloFlare, 0.25f);
		}
		Vector d = dir;
		if (_cfg.SwordModelFlip)
		{
			d = new Vector(0f - d.X, 0f - d.Y, 0f - d.Z);
		}
		float pitch = (0f - MathF.Asin(Math.Clamp(d.Z, -1f, 1f))) * 180f / MathF.PI;
		float yaw = MathF.Atan2(d.Y, d.X) * 180f / MathF.PI;
		// 模型原点在剑柄内侧（DMX：X -22.6..+62.9，刀尖在 +X）→ 沿轴前移，让剑柄落在剑基座上。
		float offset = _cfg.SwordModelOffset * MathF.Max(_cfg.SwordModelScale, 0.1f);
		Vector place = new Vector(basePos.X + d.X * offset, basePos.Y + d.Y * offset, basePos.Z + d.Z * offset);
		((CBaseEntity)_model).Teleport(place, new QAngle(pitch, yaw, _cfg.SwordModelRoll), null);
	}

	/// <summary>拆掉实体剑（不是 CBeam，不能进分帧拆 beam 的队列；只有 1 个实体，直接删）。</summary>
	private void KillSwordModel()
	{
		if (_model != null)
		{
			try
			{
				Entities.RemoveEntity(_model);
			}
			catch
			{
			}
			_model = null;
		}
	}

	/// <summary>
	/// 松手释放：未落位段强制吸附 → 法阵 / 汇聚流 / 冲天光柱退役 → 进入 0.12s 挥落
	/// （对城光带由 dice 层在挥落结束后创建 <see cref="ExcaliburBeam"/>）。方向在这一刻锁定。
	/// </summary>
	public void Release(Vector origin, Vector forward, float now)
	{
		if (_stage != Stage.Charge)
		{
			return;
		}
		_stage = Stage.Swing;
		_releaseTime = now;
		Vector f = Vectors.Normalize(forward);
		_forward = (f.X == 0f && f.Y == 0f && f.Z == 0f) ? new Vector(1f, 0f, 0f) : f;
		_chargeLength = MathF.Max(_chargeLength, 20f);

		foreach (Segment seg in _segments)
		{
			seg.Landed = true;
			seg.LandedAt = now - 1f;
		}

		// 法阵 / 汇聚流 / 冲天光柱退役（交给分帧拆除队列）
		Retire(_sigil);
		Retire(_sigil2);
		List<CBeam?> streaks = new List<CBeam?>();
		for (int i = 0; i < _streaks.Length; i++)
		{
			if (_streaks[i] != null)
			{
				streaks.Add(_streaks[i]);
				_streaks[i] = null;
			}
		}
		if (_topGlow != null)
		{
			streaks.Add(_topGlow);
			_topGlow = null;
		}
		if (_topCore != null)
		{
			streaks.Add(_topCore);
			_topCore = null;
		}
		if (streaks.Count > 0)
		{
			_retired.Add(new BeamPack(streaks));
		}
	}

	/// <summary>推进挥落 / 光柱扩张保持 / 淡出；返回 false = 全部结束（调用方应立即收尾）。</summary>
	public bool Update(Vector origin, float now)
	{
		switch (_stage)
		{
		case Stage.Charge:
			return true;
		case Stage.Swing:
		{
			float t = Math.Clamp((now - _releaseTime) / SwingSeconds, 0f, 1f);
			float te = t * t;
			Vector dir = BlendUpToForward(_forward, te);
			float length = _chargeLength;
			float segGap = length / _segments.Count;
			float half = segGap * SegmentOverlap * 0.5f;
			Vector basePos = SwordBase(origin, _forward);
			foreach (Segment seg in _segments)
			{
				if (seg.Beam == null)
				{
					continue;
				}
				float d = (seg.Index + 0.5f) * segGap;
				Vector mid = new Vector(basePos.X + dir.X * d, basePos.Y + dir.Y * d, basePos.Z + dir.Z * d);
				BeamFx.Move(seg.Beam,
					new Vector(mid.X - dir.X * half, mid.Y - dir.Y * half, mid.Z - dir.Z * half),
					new Vector(mid.X + dir.X * half, mid.Y + dir.Y * half, mid.Z + dir.Z * half));
			}
			if (_core != null)
			{
				BeamFx.Move(_core, basePos, new Vector(basePos.X + dir.X * length, basePos.Y + dir.Y * length, basePos.Z + dir.Z * length));
			}
			if (_glow != null)
			{
				BeamFx.Move(_glow, basePos, new Vector(basePos.X + dir.X * length, basePos.Y + dir.Y * length, basePos.Z + dir.Z * length));
			}
			// 剑格 / 剑尖 / 刻度跟随挥落方向（冲天光柱已退役）
			UpdateSwordOrnaments(basePos, length, dir, 1f);
			UpdateSwordModel(basePos, dir, 1f, length);
			if (t >= 1f)
			{
				// 剑退役（段 + 白芯 + 外辉光 + 剑格/剑尖/刻度）→ 对城光带由 dice 层接管
				List<CBeam?> swordBeams = new List<CBeam?>();
				foreach (Segment seg in _segments)
				{
					if (seg.Beam != null)
					{
						swordBeams.Add(seg.Beam);
						seg.Beam = null;
					}
				}
				if (_core != null)
				{
					swordBeams.Add(_core);
					_core = null;
				}
				if (_glow != null)
				{
					swordBeams.Add(_glow);
					_glow = null;
				}
				foreach (CBeam? b in _crossguard)
				{
					if (b != null)
					{
						swordBeams.Add(b);
					}
				}
				foreach (CBeam? b in _tipCross)
				{
					if (b != null)
					{
						swordBeams.Add(b);
					}
				}
				foreach (CBeam? b in _runes)
				{
					if (b != null)
					{
						swordBeams.Add(b);
					}
				}
				if (swordBeams.Count > 0)
				{
					_retired.Add(new BeamPack(swordBeams));
				}
				// 实体剑也在这时退场（对城光带接管）
				KillSwordModel();
				_stage = Stage.Done;
				return false;
			}
			return true;
		}
		default:
			return false;
		}
	}

	/// <summary>取出「已退役、等待分帧拆除」的组（调用方每 tick 取一次并交给 SigilTeardown）。</summary>
	public List<IBeamGroup> TakeRetired()
	{
		if (_retired.Count == 0)
		{
			return _empty;
		}
		List<IBeamGroup> list = new List<IBeamGroup>(_retired);
		_retired.Clear();
		return list;
	}

	/// <summary>蓄力取消：把所有还活着的组件打包返回（调用方交给分帧队列），自身变空。</summary>
	public List<IBeamGroup> RetireAll()
	{
		List<IBeamGroup> list = new List<IBeamGroup>();
		if (!_sigil.IsEmpty)
		{
			list.Add(_sigil);
		}
		if (!_sigil2.IsEmpty)
		{
			list.Add(_sigil2);
		}
		List<CBeam?> pack = new List<CBeam?>();
		foreach (Segment seg in _segments)
		{
			if (seg.Beam != null)
			{
				pack.Add(seg.Beam);
				seg.Beam = null;
			}
		}
		for (int i = 0; i < _streaks.Length; i++)
		{
			if (_streaks[i] != null)
			{
				pack.Add(_streaks[i]);
				_streaks[i] = null;
			}
		}
		if (_core != null)
		{
			pack.Add(_core);
			_core = null;
		}
		if (_glow != null)
		{
			pack.Add(_glow);
			_glow = null;
		}
		if (_topGlow != null)
		{
			pack.Add(_topGlow);
			_topGlow = null;
		}
		if (_topCore != null)
		{
			pack.Add(_topCore);
			_topCore = null;
		}
		foreach (CBeam? b in _crossguard)
		{
			pack.Add(b);
		}
		foreach (CBeam? b in _tipCross)
		{
			pack.Add(b);
		}
		foreach (CBeam? b in _runes)
		{
			pack.Add(b);
		}
		if (pack.Count > 0)
		{
			list.Add(new BeamPack(pack));
		}
		if (_retired.Count > 0)
		{
			list.AddRange(_retired);
			_retired.Clear();
		}
		KillSwordModel();
		_stage = Stage.Done;
		return list;
	}

	private void Retire(IBeamGroup group)
	{
		if (!group.IsEmpty)
		{
			_retired.Add(group);
		}
	}

	public void Remove()
	{
		foreach (Segment seg in _segments)
		{
			BeamFx.Kill(seg.Beam);
			seg.Beam = null;
		}
		for (int i = 0; i < _streaks.Length; i++)
		{
			BeamFx.Kill(_streaks[i]);
			_streaks[i] = null;
		}
		BeamFx.Kill(_core);
		_core = null;
		BeamFx.Kill(_glow);
		_glow = null;
		BeamFx.Kill(_topGlow);
		_topGlow = null;
		BeamFx.Kill(_topCore);
		_topCore = null;
		foreach (CBeam? b in _crossguard)
		{
			BeamFx.Kill(b);
		}
		foreach (CBeam? b in _tipCross)
		{
			BeamFx.Kill(b);
		}
		foreach (CBeam? b in _runes)
		{
			BeamFx.Kill(b);
		}
		_sigil.Remove();
		_sigil2.Remove();
		_retired.Clear();
		KillSwordModel();
		_stage = Stage.Done;
	}

	public bool IsEmpty
	{
		get
		{
			if (_retired.Count > 0)
			{
				return false;
			}
			if (_model != null && _model.IsValid)
			{
				return false;
			}
			if (_core != null || _glow != null || _topGlow != null || _topCore != null)
			{
				return false;
			}
			if (!_sigil.IsEmpty || !_sigil2.IsEmpty)
			{
				return false;
			}
			foreach (CBeam? b in _crossguard)
			{
				if (BeamFx.IsAlive(b))
				{
					return false;
				}
			}
			foreach (CBeam? b in _tipCross)
			{
				if (BeamFx.IsAlive(b))
				{
					return false;
				}
			}
			foreach (CBeam? b in _runes)
			{
				if (BeamFx.IsAlive(b))
				{
					return false;
				}
			}
			foreach (Segment seg in _segments)
			{
				if (BeamFx.IsAlive(seg.Beam))
				{
					return false;
				}
			}
			for (int i = 0; i < _streaks.Length; i++)
			{
				if (BeamFx.IsAlive(_streaks[i]))
				{
					return false;
				}
			}
			return true;
		}
	}

	public int RemoveChunk(int budget)
	{
		Remove();
		return 1 << 20;
	}

	/// <summary>还活着的 CBeam 根数（调试回显用）。</summary>
	public int BeamCount
	{
		get
		{
			int n = 0;
			foreach (Segment seg in _segments)
			{
				if (BeamFx.IsAlive(seg.Beam))
				{
					n++;
				}
			}
			for (int i = 0; i < _streaks.Length; i++)
			{
				if (BeamFx.IsAlive(_streaks[i]))
				{
					n++;
				}
			}
			if (BeamFx.IsAlive(_core))
			{
				n++;
			}
			if (BeamFx.IsAlive(_glow))
			{
				n++;
			}
			if (BeamFx.IsAlive(_topGlow))
			{
				n++;
			}
			if (BeamFx.IsAlive(_topCore))
			{
				n++;
			}
			foreach (CBeam? b in _crossguard)
			{
				if (BeamFx.IsAlive(b))
				{
					n++;
				}
			}
			foreach (CBeam? b in _tipCross)
			{
				if (BeamFx.IsAlive(b))
				{
					n++;
				}
			}
			foreach (CBeam? b in _runes)
			{
				if (BeamFx.IsAlive(b))
				{
					n++;
				}
			}
			return n + _sigil.BeamCount + _sigil2.BeamCount;
		}
	}

	private static Vector Bezier(Vector a, Vector b, Vector c, float t)
	{
		float u = 1f - t;
		float uu = u * u;
		float tt = t * t;
		float ut2 = 2f * u * t;
		return new Vector(
			a.X * uu + b.X * ut2 + c.X * tt,
			a.Y * uu + b.Y * ut2 + c.Y * tt,
			a.Z * uu + b.Z * ut2 + c.Z * tt);
	}

	private static Vector BlendUpToForward(Vector forward, float t)
	{
		Vector v = new Vector(forward.X * t, forward.Y * t, (1f - t) + forward.Z * t);
		Vector n = Vectors.Normalize(v);
		if (n.X == 0f && n.Y == 0f && n.Z == 0f)
		{
			return forward;
		}
		return n;
	}

	/// <summary>剑基座：玩家右前方、头部高度（竖直剑立在右手边，第一人称平视可见）。</summary>
	private static Vector SwordBase(Vector origin, Vector forward)
	{
		Vector fwdH = Vectors.Normalize(new Vector(forward.X, forward.Y, 0f));
		if (fwdH.X == 0f && fwdH.Y == 0f && fwdH.Z == 0f)
		{
			fwdH = new Vector(1f, 0f, 0f);
		}
		Vector rightH = new Vector(fwdH.Y, 0f - fwdH.X, 0f);
		return new Vector(
			origin.X + rightH.X * SwordSideOffset + fwdH.X * SwordForwardOffset,
			origin.Y + rightH.Y * SwordSideOffset + fwdH.Y * SwordForwardOffset,
			origin.Z + HeadOffset);
	}
}
