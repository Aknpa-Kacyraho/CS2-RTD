#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 一组可整体销毁的 CBeam 实体（法阵 / 穹顶 / 法阵群 / 冲击环）。
/// 统一抽出来是为了让 <see cref="SigilTeardown"/> 能对**任意** beam 组合做分帧拆除，
/// 避免"同帧批量删上千 CBeam"触发引擎原生崩溃。
/// </summary>
public interface IBeamGroup
{
	bool IsEmpty { get; }

	/// <summary>最多拆掉约 <paramref name="budget"/> 根 CBeam，返回实际拆掉的根数。</summary>
	int RemoveChunk(int budget);

	void Remove();
}

/// <summary>
/// 法阵刻画的确定性伪随机：同一 (seed, index) 结果恒定，用于符文带的长短与缺口。
/// 不追求密码学强度，只要"像手写铭文"且可复现。
/// </summary>
public static class SigilGeometry
{
	public static float Hash01(int seed, int index)
	{
		unchecked
		{
			ulong x = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + (ulong)(index + 1) * 0xBF58476D1CE4E5B9UL;
			x ^= x >> 30;
			x *= 0xBF58476D1CE4E5B9UL;
			x ^= x >> 27;
			x *= 0x94D049BB133111EBUL;
			x ^= x >> 31;
			return (x & 0xFFFFFFu) / 16777216f;
		}
	}
}

/// <summary>法阵密度/样式参数（由 dice config 提供）。</summary>
public sealed class SigilParams
{
	public float Density { get; set; } = 1f;
	public int RuneTicks { get; set; } = 84;
	public int TickRingCount { get; set; } = 42;
	public int StarPoints { get; set; } = 5;
	public int StarSkip { get; set; } = 2;
	public int PolygonSides { get; set; } = 8;
	public bool DoubleLine { get; set; } = true;
	public int Seed { get; set; }

	/// <summary>按全局密度缩放一个"基数"，并夹到 [min, max]。</summary>
	public int Scaled(int baseCount, int min = 1, int max = 512)
	{
		int v = (int)MathF.Round(baseCount * Math.Clamp(Density, 0.5f, 2f));
		return Math.Clamp(v, min, max);
	}

	public static SigilParams From(float density, int runeTicks, int tickRingCount, int starPoints, int starSkip, int polygonSides, bool doubleLine, int seed)
	{
		return new SigilParams
		{
			Density = density,
			RuneTicks = runeTicks,
			TickRingCount = tickRingCount,
			StarPoints = starPoints,
			StarSkip = starSkip,
			PolygonSides = polygonSides,
			DoubleLine = doubleLine,
			Seed = seed
		};
	}
}

/// <summary>三色配色：亮芯 / 中色 / 暗晕。</summary>
public readonly struct SigilPalette
{
	public readonly Color Core;
	public readonly Color Mid;
	public readonly Color Halo;

	public SigilPalette(Color core, Color mid, Color halo)
	{
		Core = core;
		Mid = mid;
		Halo = halo;
	}
}

/// <summary>
/// 一个"刻画"图元：一组极坐标线段（每段 = (r1,a1) → (r2,a2) + 高度 z）+ 自转。
///
/// <para><see cref="Animated"/>=false 表示该图元旋转对称（圆环），转动不可见 → 容器可跳过每 tick 更新，
/// 只在跟随点/缩放变化时重摆。这是"实体数变多、每 tick 成本不变"的关键。</para>
/// </summary>
public sealed class SigilShape
{
	private readonly struct Seg
	{
		public readonly float R1;
		public readonly float A1;
		public readonly float R2;
		public readonly float A2;
		public readonly float Z;
		public readonly CBeam Core;
		public readonly CBeam? Halo;

		public Seg(float r1, float a1, float r2, float a2, float z, CBeam core, CBeam? halo)
		{
			R1 = r1;
			A1 = a1;
			R2 = r2;
			A2 = a2;
			Z = z;
			Core = core;
			Halo = halo;
		}
	}

	private readonly List<Seg> _segs = new List<Seg>();
	private readonly Color _core;
	private readonly Color _halo;
	private readonly float _width;
	private readonly bool _hasHalo;

	public bool Animated { get; }
	public float Spin { get; }
	public int BeamCount { get; private set; }

	public SigilShape(bool animated, float spin, Color core, Color halo, float width, bool haloEnabled)
	{
		Animated = animated;
		Spin = spin;
		_core = core;
		_halo = halo;
		_width = width;
		_hasHalo = haloEnabled;
	}

	/// <summary>追加一段：从 (r1, a1) 到 (r2, a2)，高度偏移 z。建完由调用方 <see cref="Update"/> 落位。</summary>
	public void Add(float r1, float a1, float r2, float a2, float z)
	{
		CBeam? core = BeamFx.Create(_core, _width);
		if (core == null)
		{
			return;
		}
		CBeam? halo = null;
		if (_hasHalo)
		{
			halo = BeamFx.Create(_halo, _width * 2.4f);
		}
		_segs.Add(new Seg(r1, a1, r2, a2, z, core, halo));
		BeamCount += (halo != null) ? 2 : 1;
	}

	public void Update(Vector center, float time, float scale)
	{
		float rot = time * Spin;
		foreach (Seg s in _segs)
		{
			Vector p1 = World(center, s.R1 * scale, s.A1 + rot, s.Z);
			Vector p2 = World(center, s.R2 * scale, s.A2 + rot, s.Z);
			BeamFx.Move(s.Core, p1, p2);
			if (s.Halo != null)
			{
				BeamFx.Move(s.Halo, p1, p2);
			}
		}
	}

	public void Remove()
	{
		foreach (Seg s in _segs)
		{
			BeamFx.Kill(s.Core);
			BeamFx.Kill(s.Halo);
		}
		_segs.Clear();
		BeamCount = 0;
	}

	private static Vector World(Vector c, float radius, float angle, float z)
	{
		return new Vector(c.X + MathF.Cos(angle) * radius, c.Y + MathF.Sin(angle) * radius, c.Z + z);
	}
}

/// <summary>法阵刻画原语的工厂：圆 / 星 / 多边形 / 辐条 / 刻度环 / 符文带。</summary>
public static class SigilBuilder
{
	/// <summary>圆环（旋转对称 → 静态）。</summary>
	public static SigilShape Arc(Vector center, float radius, int segments, float z, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		segments = Math.Clamp(segments, 3, 256);
		SigilShape s = new SigilShape(false, 0f, core, halo, width, haloEnabled);
		for (int i = 0; i < segments; i++)
		{
			float a = phase + i * 2f * MathF.PI / segments;
			float b = phase + (i + 1) * 2f * MathF.PI / segments;
			s.Add(radius, a, radius, b, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>星形多边形 {points/skip}：五芒星 = {5/2}，六芒星 = {6/2}。</summary>
	public static SigilShape Star(Vector center, float radius, int points, int skip, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		points = Math.Clamp(points, 3, 24);
		skip = Math.Clamp(skip, 1, points - 1);
		SigilShape s = new SigilShape(true, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < points; i++)
		{
			float a = phase + i * 2f * MathF.PI / points;
			float b = phase + (i + skip) % points * 2f * MathF.PI / points;
			s.Add(radius, a, radius, b, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>正多边形（逐边相连）。</summary>
	public static SigilShape Polygon(Vector center, float radius, int sides, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		sides = Math.Clamp(sides, 3, 24);
		SigilShape s = new SigilShape(true, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < sides; i++)
		{
			float a = phase + i * 2f * MathF.PI / sides;
			float b = phase + (i + 1) * 2f * MathF.PI / sides;
			s.Add(radius, a, radius, b, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>辐条：给定角度表，从 fromRadius 拉到 toRadius。</summary>
	public static SigilShape Spokes(Vector center, float fromRadius, float toRadius, IReadOnlyList<float> angles, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		SigilShape s = new SigilShape(true, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < angles.Count; i++)
		{
			float a = phase + angles[i];
			s.Add(fromRadius, a, toRadius, a, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>刻度环：一圈长短交替的径向刻度（像表盘）。</summary>
	public static SigilShape TickRing(Vector center, float radius, int count, float length, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 4, 256);
		SigilShape s = new SigilShape(true, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			float a = phase + i * 2f * MathF.PI / count;
			float len = length * ((i % 2 == 0) ? 1f : 0.62f);
			s.Add(radius - len, a, radius, a, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>
	/// 符文带：两道同心半径之间的一圈短刻度，长度不一、带随机缺口、有内外朝向之分 → 像一圈铭文。
	/// 这是"密"感的主要来源。
	/// </summary>
	public static SigilShape RuneBand(Vector center, float innerRadius, float outerRadius, int count, float z, float spin, int seed, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 4, 400);
		float band = MathF.Max(outerRadius - innerRadius, 1f);
		float step = 2f * MathF.PI / count;
		SigilShape s = new SigilShape(true, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			if (SigilGeometry.Hash01(seed, i) < 0.12f)
			{
				continue;
			}
			float a = phase + i * step + (SigilGeometry.Hash01(seed, i + 3000) - 0.5f) * step * 0.5f;
			float len = 0.5f + 0.5f * SigilGeometry.Hash01(seed, i + 6000);
			if (SigilGeometry.Hash01(seed, i + 9000) > 0.5f)
			{
				s.Add(innerRadius, a, innerRadius + band * len, a, z);
			}
			else
			{
				s.Add(outerRadius - band * len, a, outerRadius, a, z);
			}
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>把 count 个顶点均分到圆上的角度表（供辐条用）。</summary>
	public static float[] StarAngles(int count, float phase = 0f)
	{
		count = Math.Max(count, 1);
		float[] angles = new float[count];
		for (int i = 0; i < count; i++)
		{
			angles[i] = phase + i * 2f * MathF.PI / count;
		}
		return angles;
	}
}
