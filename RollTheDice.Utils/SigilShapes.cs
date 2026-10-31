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
		public readonly float Z1;
		public readonly float R2;
		public readonly float A2;
		public readonly float Z2;
		public readonly CBeam Core;
		public readonly CBeam? Halo;

		public Seg(float r1, float a1, float z1, float r2, float a2, float z2, CBeam core, CBeam? halo)
		{
			R1 = r1;
			A1 = a1;
			Z1 = z1;
			R2 = r2;
			A2 = a2;
			Z2 = z2;
			Core = core;
			Halo = halo;
		}
	}

	private readonly List<Seg> _segs = new List<Seg>();
	private readonly Color _core;
	private readonly Color _halo;
	private readonly float _width;
	private readonly bool _hasHalo;
	private float _pivotX;
	private float _pivotY;
	private float _pivotZ;
	private float _axisX;
	private float _axisY;
	private float _axisZ = 1f;
	private bool _customFrame;

	public bool Animated { get; }
	public float Spin { get; }
	public int BeamCount { get; private set; }

	/// <summary>
	/// 计数模式：只统计"这段刻画会创建多少根 CBeam"、不真的建实体。
	/// 懒建法阵（<see cref="SigilDome"/>）在构造时用它预告总束数，避免为了回显而先把实体铺出来。
	/// </summary>
	public static bool CountingOnly { get; set; }

	public SigilShape(bool animated, float spin, Color core, Color halo, float width, bool haloEnabled)
	{
		Animated = animated;
		Spin = spin;
		_core = core;
		_halo = halo;
		_width = width;
		_hasHalo = haloEnabled;
	}

	/// <summary>追加一段水平线段：从 (r1, a1) 到 (r2, a2)，高度偏移 z。</summary>
	public void Add(float r1, float a1, float r2, float a2, float z)
	{
		Add(r1, a1, z, r2, a2, z);
	}

	/// <summary>追加一段**任意空间**线段（相对图元中心的笛卡尔坐标）：倾斜环 / 弧形轨道 / 立体核心用。</summary>
	public void AddLocal(float x1, float y1, float z1, float x2, float y2, float z2)
	{
		Add(MathF.Sqrt(x1 * x1 + y1 * y1), MathF.Atan2(y1, x1), z1,
			MathF.Sqrt(x2 * x2 + y2 * y2), MathF.Atan2(y2, x2), z2);
	}

	/// <summary>
	/// 自转参考系：默认 (pivot=0, axis=+Z) 时自转 = 绕"中心竖直轴"（原行为，等价于 a+rot）；
	/// 倾斜侧阵门传 <c>(门心偏移, 门平面法线)</c> → 自转发生在门自身平面内，而不是绕中心公转。
	/// </summary>
	public void SetSpinFrame(float px, float py, float pz, float ax, float ay, float az)
	{
		float len = MathF.Sqrt(ax * ax + ay * ay + az * az);
		if (len < 1e-5f)
		{
			return;
		}
		_pivotX = px;
		_pivotY = py;
		_pivotZ = pz;
		_axisX = ax / len;
		_axisY = ay / len;
		_axisZ = az / len;
		_customFrame = true;
	}

	/// <summary>追加一段**三维**线段：两端各自的半径 / 角度 / 高度 → 斜向锚线、螺旋、竖柱都靠它。</summary>
	public void Add(float r1, float a1, float z1, float r2, float a2, float z2)
	{
		if (CountingOnly)
		{
			BeamCount += (_hasHalo ? 2 : 1);
			return;
		}
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
		_segs.Add(new Seg(r1, a1, z1, r2, a2, z2, core, halo));
		BeamCount += (halo != null) ? 2 : 1;
	}

	public void Update(Vector center, float time, float scale)
	{
		float rot = time * Spin;
		if (!_customFrame)
		{
			foreach (Seg s in _segs)
			{
				Vector p1 = World(center, s.R1 * scale, s.A1 + rot, s.Z1);
				Vector p2 = World(center, s.R2 * scale, s.A2 + rot, s.Z2);
				BeamFx.Move(s.Core, p1, p2);
				if (s.Halo != null)
				{
					BeamFx.Move(s.Halo, p1, p2);
				}
			}
			return;
		}
		float cos = MathF.Cos(rot);
		float sin = MathF.Sin(rot);
		foreach (Seg s in _segs)
		{
			Vector p1 = Framed(center, s.R1 * scale, s.A1, s.Z1, cos, sin);
			Vector p2 = Framed(center, s.R2 * scale, s.A2, s.Z2, cos, sin);
			BeamFx.Move(s.Core, p1, p2);
			if (s.Halo != null)
			{
				BeamFx.Move(s.Halo, p1, p2);
			}
		}
	}

	/// <summary>把局部点 (r, a, z) 绕参考系的轴 / 枢轴转 rot（Rodrigues），再平移到世界坐标。</summary>
	private Vector Framed(Vector center, float radius, float angle, float z, float cos, float sin)
	{
		float x = MathF.Cos(angle) * radius;
		float y = MathF.Sin(angle) * radius;
		float px = x - _pivotX;
		float py = y - _pivotY;
		float pz = z - _pivotZ;
		float dot = _axisX * px + _axisY * py + _axisZ * pz;
		float cx = _axisY * pz - _axisZ * py;
		float cy = _axisZ * px - _axisX * pz;
		float cz = _axisX * py - _axisY * px;
		float omc = 1f - cos;
		return new Vector(
			center.X + _pivotX + px * cos + cx * sin + _axisX * dot * omc,
			center.Y + _pivotY + py * cos + cy * sin + _axisY * dot * omc,
			center.Z + _pivotZ + pz * cos + cz * sin + _axisZ * dot * omc);
	}

	/// <summary>运行时整体加宽（脉冲信号用）：<paramref name="scale"/> 是相对原线宽的倍率。</summary>
	public void SetWidthScale(float scale)
	{
		foreach (Seg s in _segs)
		{
			BeamFx.SetWidth(s.Core, _width * scale);
			if (s.Halo != null)
			{
				BeamFx.SetWidth(s.Halo, _width * 2.4f * scale);
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

/// <summary>环上标记的形状（图 1/2/3 都在用的小图元）。</summary>
public enum MarkerShape
{
	Dot,
	Square,
	Diamond,
	Triangle,
	Cross
}

/// <summary>
/// 一组 <see cref="SigilShape"/> 的容器（法阵的一层 / 调试法阵 / 地面主法阵）：
/// 统一承担"旋转对称的静态图元只在跟随点或缩放变化时重摆"的省流逻辑。
/// </summary>
public sealed class SigilShapeGroup : IBeamGroup
{
	private readonly List<SigilShape> _shapes = new List<SigilShape>();
	private readonly List<SigilShape> _static = new List<SigilShape>();
	private Vector? _lastCenter;
	private float _lastScale = -1f;

	public int BeamCount { get; private set; }

	/// <summary>
	/// 自转图元的更新间隔（tick）：1 = 每 tick 更新，2 = 隔一 tick（默认级别，**每 tick 网络成本直接减半**，
	/// 32Hz 的自转肉眼几乎看不出）。静态图元不受影响（它们本来就只在跟随点/缩放变化时重摆）。
	/// </summary>
	public int Stride { get; set; } = 1;

	/// <summary>容器内的刻画（供脉冲等整体操作遍历）。</summary>
	public IReadOnlyList<SigilShape> Shapes => _shapes;

	public void Add(SigilShape shape)
	{
		_shapes.Add(shape);
		if (!shape.Animated)
		{
			_static.Add(shape);
		}
		BeamCount += shape.BeamCount;
	}

	public void AddRange(IEnumerable<SigilShape> shapes)
	{
		foreach (SigilShape shape in shapes)
		{
			Add(shape);
		}
	}

	public void Update(Vector center, float time, float scale = 1f)
	{
		if (scale <= 0f)
		{
			scale = 0.01f;
		}
		// 降频：Stride>1 时自转图元隔 N 个 tick 才 Move 一次（角度按 time 算，不会漂移）。
		bool animateNow = Stride <= 1 || (int)(time * 64f) % Stride == 0;
		if (animateNow)
		{
			foreach (SigilShape shape in _shapes)
			{
				if (shape.Animated)
				{
					shape.Update(center, time, scale);
				}
			}
		}
		bool statics = _lastCenter == null || MathF.Abs(scale - _lastScale) > 0.004f || Vectors.GetDistance(_lastCenter, center) > 1f;
		if (statics)
		{
			foreach (SigilShape shape in _static)
			{
				shape.Update(center, time, scale);
			}
			_lastScale = scale;
			_lastCenter = new Vector(center.X, center.Y, center.Z);
		}
	}

	public void Remove()
	{
		foreach (SigilShape shape in _shapes)
		{
			shape.Remove();
		}
		_shapes.Clear();
		_static.Clear();
		BeamCount = 0;
	}

	public bool IsEmpty => _shapes.Count == 0;

	public int RemoveChunk(int budget)
	{
		int removed = 0;
		while (_shapes.Count > 0 && removed < budget)
		{
			int last = _shapes.Count - 1;
			SigilShape shape = _shapes[last];
			_shapes.RemoveAt(last);
			_static.Remove(shape);
			removed += shape.BeamCount;
			BeamCount -= shape.BeamCount;
			shape.Remove();
		}
		return removed;
	}
}

/// <summary>法阵刻画原语的工厂：圆 / 方框 / 星 / 多边形 / 辐条 / 刻度环 / 符文带 / 标记 / 本轮 / 光芒。</summary>
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
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
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
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
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
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < angles.Count; i++)
		{
			float a = phase + angles[i];
			s.Add(fromRadius, a, toRadius, a, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>斜向辐条（3D）：从 (fromRadius, zFrom) 拉到 (toRadius, zTo) —— 空间锚定线 / 斜撑 / 竖柱用。</summary>
	public static SigilShape Spokes3D(Vector center, float fromRadius, float toRadius, float zFrom, float zTo, IReadOnlyList<float> angles, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < angles.Count; i++)
		{
			float a = phase + angles[i];
			s.Add(fromRadius, a, zFrom, toRadius, a, zTo);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>刻度环：一圈长短交替的径向刻度（像表盘）。</summary>
	public static SigilShape TickRing(Vector center, float radius, int count, float length, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 4, 256);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
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
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
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

	// ---- 参考图新增原语（方阵封印 / 星盘 / 太阳） ----

	/// <summary>方框（图 1 的封印外框）：<paramref name="radius"/> 为外接圆半径（角到中心）。</summary>
	public static SigilShape Square(Vector center, float radius, int segmentsPerSide, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		segmentsPerSide = Math.Clamp(segmentsPerSide, 2, 64);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int k = 0; k < 4; k++)
		{
			float a1 = phase + k * MathF.PI * 0.5f;
			float a2 = a1 + MathF.PI * 0.5f;
			float x1 = MathF.Cos(a1) * radius;
			float y1 = MathF.Sin(a1) * radius;
			float x2 = MathF.Cos(a2) * radius;
			float y2 = MathF.Sin(a2) * radius;
			for (int i = 0; i < segmentsPerSide; i++)
			{
				float t1 = i / (float)segmentsPerSide;
				float t2 = (i + 1) / (float)segmentsPerSide;
				AddCartesian(s, x1 + (x2 - x1) * t1, y1 + (y2 - y1) * t1, x1 + (x2 - x1) * t2, y1 + (y2 - y1) * t2, z);
			}
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>方框四角括号（图 1 的 ⌐ 角标）：每角沿两条边各伸出一段，<paramref name="offset"/> 为向外偏移。</summary>
	public static SigilShape CornerBrackets(Vector center, float radius, float arm, float offset, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int k = 0; k < 4; k++)
		{
			float a = phase + k * MathF.PI * 0.5f;
			float ux = MathF.Cos(a);
			float uy = MathF.Sin(a);
			float xk = ux * radius;
			float yk = uy * radius;
			float d1x = MathF.Cos(a + MathF.PI * 0.5f) * radius - xk;
			float d1y = MathF.Sin(a + MathF.PI * 0.5f) * radius - yk;
			float n1 = MathF.Max(MathF.Sqrt(d1x * d1x + d1y * d1y), 0.001f);
			float d2x = MathF.Cos(a - MathF.PI * 0.5f) * radius - xk;
			float d2y = MathF.Sin(a - MathF.PI * 0.5f) * radius - yk;
			float n2 = MathF.Max(MathF.Sqrt(d2x * d2x + d2y * d2y), 0.001f);
			float cx = xk + ux * offset;
			float cy = yk + uy * offset;
			AddCartesian(s, cx, cy, cx + d1x / n1 * arm, cy + d1y / n1 * arm, z);
			AddCartesian(s, cx, cy, cx + d2x / n2 * arm, cy + d2y / n2 * arm, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>尖角罗盘星（图 1 中心 / 图 3）：顶点在 rOuter、内凹点在 rInner，<paramref name="fill"/> 控制角宽窄。</summary>
	public static SigilShape CompassStar(Vector center, float rOuter, float rInner, int points, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f, float fill = 1f)
	{
		points = Math.Clamp(points, 3, 24);
		float step = 2f * MathF.PI / points;
		float half = step * 0.5f * Math.Clamp(fill, 0.2f, 1f);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < points; i++)
		{
			float a = phase + i * step;
			s.Add(rInner, a - half, rOuter, a, z);
			s.Add(rOuter, a, rInner, a + half, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>局部圆弧（图 2 的月牙弧 / 漩涡弧）：只画 a1→a2 一段。</summary>
	public static SigilShape ArcSpan(Vector center, float radius, float a1, float a2, int segments, float z, float spin, Color core, Color halo, float width, bool haloEnabled)
	{
		segments = Math.Clamp(segments, 1, 128);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < segments; i++)
		{
			float t1 = i / (float)segments;
			float t2 = (i + 1) / (float)segments;
			s.Add(radius, a1 + (a2 - a1) * t1, radius, a1 + (a2 - a1) * t2, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>环上小圆（图 2 的"本轮"）：在 radius 环上均分 count 个半径 circleRadius 的小圆。</summary>
	public static SigilShape Epicycle(Vector center, float radius, int count, float circleRadius, int segments, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 1, 24);
		segments = Math.Clamp(segments, 3, 32);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			float a = phase + i * 2f * MathF.PI / count;
			float cx = MathF.Cos(a) * radius;
			float cy = MathF.Sin(a) * radius;
			for (int k = 0; k < segments; k++)
			{
				float b1 = k * 2f * MathF.PI / segments;
				float b2 = (k + 1) * 2f * MathF.PI / segments;
				AddCartesian(s, cx + MathF.Cos(b1) * circleRadius, cy + MathF.Sin(b1) * circleRadius,
					cx + MathF.Cos(b2) * circleRadius, cy + MathF.Sin(b2) * circleRadius, z);
			}
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>偏心圆环：圆心在 (offsetX, offsetY)（相对法阵中心・跟随朝向不动），"侧挂小阵"用。</summary>
	public static SigilShape RingAt(Vector center, float offsetX, float offsetY, float radius, int segments, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		segments = Math.Clamp(segments, 4, 96);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < segments; i++)
		{
			float a1 = phase + i * 2f * MathF.PI / segments;
			float a2 = phase + (i + 1) * 2f * MathF.PI / segments;
			AddCartesian(s, offsetX + MathF.Cos(a1) * radius, offsetY + MathF.Sin(a1) * radius,
				offsetX + MathF.Cos(a2) * radius, offsetY + MathF.Sin(a2) * radius, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>环上标记（图 1 边中点方块 / 图 2 菱形与圆点 / 图 3 三角刻度）：在 radius 环上均分 count 个标记。</summary>
	public static SigilShape MarkerRing(Vector center, float radius, int count, MarkerShape shape, float size, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 1, 64);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			Marker(s, radius, phase + i * 2f * MathF.PI / count, shape, MathF.Max(size, 0.5f), z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>外扩光芒（图 3 的太阳射线）：从 fromRadius 射到 toRadius，长短交替，可伸出法阵本体之外。</summary>
	public static SigilShape Corona(Vector center, float fromRadius, float toRadius, int count, float z, float spin, Color core, Color halo, float width, bool haloEnabled, float phase = 0f, float shortRatio = 0.68f)
	{
		count = Math.Clamp(count, 3, 96);
		float from = MathF.Max(fromRadius, 1f);
		float to = MathF.Max(toRadius, from + 1f);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			float a = phase + i * 2f * MathF.PI / count;
			float len = (to - from) * ((i % 2 == 0) ? 1f : Math.Clamp(shortRatio, 0.1f, 1f));
			s.Add(from, a, from + len, a, z);
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	// ---- 装置化原语（倾斜侧阵门 / 弧形轨道 / 立式外缘符文） ----

	/// <summary>倾斜面参考系：环心 O + 切向 v + 面内径向 w + 法线 n（供倾斜原语共用）。</summary>
	private readonly struct TiltFrame
	{
		public readonly float Ox;
		public readonly float Oy;
		public readonly float Oz;
		public readonly float Vx;
		public readonly float Vy;
		public readonly float Wx;
		public readonly float Wy;
		public readonly float Wz;
		public readonly float Nx;
		public readonly float Ny;
		public readonly float Nz;

		public TiltFrame(float offsetRadius, float offsetAngle, float z, float tilt, float normalOffset)
		{
			float ux = MathF.Cos(offsetAngle);
			float uy = MathF.Sin(offsetAngle);
			float ct = MathF.Cos(tilt);
			float st = MathF.Sin(tilt);
			// 法线朝"外上方"：tilt=0 → 法线竖直（水平环）；tilt=90° → 法线朝外（竖直环）。
			Nx = -ux * st;
			Ny = -uy * st;
			Nz = ct;
			Vx = -uy;
			Vy = ux;
			Wx = ux * ct;
			Wy = uy * ct;
			Wz = st;
			Ox = ux * offsetRadius + Nx * normalOffset;
			Oy = uy * offsetRadius + Ny * normalOffset;
			Oz = z + Nz * normalOffset;
		}

		/// <summary>面内极坐标 (t, radius) → 相对法阵中心的笛卡尔点。</summary>
		public void Point(float t, float radius, out float x, out float y, out float z)
		{
			float c = MathF.Cos(t);
			float s = MathF.Sin(t);
			x = Ox + (Vx * c + Wx * s) * radius;
			y = Oy + (Vy * c + Wy * s) * radius;
			z = Oz + Wz * s * radius;
		}
	}

	/// <summary>
	/// 倾斜圆环：环心在 (offsetRadius, offsetAngle, z)（相对法阵中心），
	/// 环面法线朝"外上方"倾斜 <paramref name="tilt"/> 弧度（0 = 水平环，π/2 = 竖直环）。
	/// <paramref name="normalOffset"/> 沿法线偏移（负 = 背面）→ 一对环 = "有厚度的盘"。
	/// </summary>
	public static SigilShape TiltedRing(Vector center, float offsetRadius, float offsetAngle, float z,
		float radius, int segments, float tilt, float normalOffset, float spin,
		Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		segments = Math.Clamp(segments, 3, 96);
		TiltFrame frame = new TiltFrame(offsetRadius, offsetAngle, z, tilt, normalOffset);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < segments; i++)
		{
			float t1 = phase + i * 2f * MathF.PI / segments;
			float t2 = phase + (i + 1) * 2f * MathF.PI / segments;
			frame.Point(t1, radius, out float x1, out float y1, out float z1);
			frame.Point(t2, radius, out float x2, out float y2, out float z2);
			s.AddLocal(x1, y1, z1, x2, y2, z2);
		}
		s.SetSpinFrame(frame.Ox, frame.Oy, frame.Oz, frame.Nx, frame.Ny, frame.Nz);
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>倾斜面内的辐条 / 刻度：从 rFrom 到 rTo 的 count 根径向线（同一倾斜面，自转在面内）。</summary>
	public static SigilShape TiltedSpokes(Vector center, float offsetRadius, float offsetAngle, float z,
		float rFrom, float rTo, int count, float tilt, float normalOffset, float spin,
		Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 1, 64);
		TiltFrame frame = new TiltFrame(offsetRadius, offsetAngle, z, tilt, normalOffset);
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			float t = phase + i * 2f * MathF.PI / count;
			frame.Point(t, rFrom, out float x1, out float y1, out float z1);
			frame.Point(t, rTo, out float x2, out float y2, out float z2);
			s.AddLocal(x1, y1, z1, x2, y2, z2);
		}
		s.SetSpinFrame(frame.Ox, frame.Oy, frame.Oz, frame.Nx, frame.Ny, frame.Nz);
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>
	/// 弧形轨道：从 (a0, r0, z0) 到 (a1, r1, z1) 的二次贝塞尔折线；
	/// 控制点在中点方位、半径 +<paramref name="bow"/>（&gt;0 向外弓）、高度取中值 → 环形轨道 / 弧形连接臂。
	/// </summary>
	public static SigilShape ArcTrack(Vector center, float a0, float r0, float z0, float a1, float r1, float z1,
		float bow, int segments, float spin, Color core, Color halo, float width, bool haloEnabled)
	{
		segments = Math.Clamp(segments, 2, 48);
		float am = a0 + ShortestAngle(a1 - a0) * 0.5f;
		float rm = (r0 + r1) * 0.5f + bow;
		float zm = (z0 + z1) * 0.5f;
		float p0x = MathF.Cos(a0) * r0;
		float p0y = MathF.Sin(a0) * r0;
		float p1x = MathF.Cos(am) * rm;
		float p1y = MathF.Sin(am) * rm;
		float p2x = MathF.Cos(a1) * r1;
		float p2y = MathF.Sin(a1) * r1;
		SigilShape s = new SigilShape(spin != 0f, spin, core, halo, width, haloEnabled);
		float prevX = p0x;
		float prevY = p0y;
		float prevZ = z0;
		for (int i = 1; i <= segments; i++)
		{
			float t = i / (float)segments;
			float it = 1f - t;
			float x = it * it * p0x + 2f * it * t * p1x + t * t * p2x;
			float y = it * it * p0y + 2f * it * t * p1y + t * t * p2y;
			float z = it * it * z0 + 2f * it * t * zm + t * t * z1;
			s.AddLocal(prevX, prevY, prevZ, x, y, z);
			prevX = x;
			prevY = y;
			prevZ = z;
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>立式外缘符文：贴在半径 r 圆柱面上的一圈竖直短刻度（长短不一、随机缺口）→ 盘外缘的铭文带。</summary>
	public static SigilShape RimTicks(Vector center, float radius, int count, float zBase, float height, int seed,
		Color core, Color halo, float width, bool haloEnabled, float phase = 0f)
	{
		count = Math.Clamp(count, 4, 200);
		SigilShape s = new SigilShape(false, 0f, core, halo, width, haloEnabled);
		for (int i = 0; i < count; i++)
		{
			if (SigilGeometry.Hash01(seed, i) < 0.10f)
			{
				continue;
			}
			float a = phase + i * 2f * MathF.PI / count;
			float len = 0.45f + 0.55f * SigilGeometry.Hash01(seed, i + 1000);
			if (SigilGeometry.Hash01(seed, i + 2000) > 0.5f)
			{
				s.Add(radius, a, zBase, radius, a, zBase + height * len);
			}
			else
			{
				s.Add(radius, a, zBase + height * (1f - len), radius, a, zBase + height);
			}
		}
		s.Update(center, 0f, 1f);
		return s;
	}

	/// <summary>把角度差规到 (-π, π]（弧形轨道取短弧中点用）。</summary>
	private static float ShortestAngle(float delta)
	{
		return delta - 2f * MathF.PI * MathF.Floor((delta + MathF.PI) / (2f * MathF.PI));
	}

	/// <summary>把一段笛卡尔坐标线段折成极坐标端点后追加（方框 / 标记 / 小圆这类非圆轨迹用）。</summary>
	private static void AddCartesian(SigilShape s, float x1, float y1, float x2, float y2, float z)
	{
		s.Add(MathF.Sqrt(x1 * x1 + y1 * y1), MathF.Atan2(y1, x1), MathF.Sqrt(x2 * x2 + y2 * y2), MathF.Atan2(y2, x2), z);
	}

	/// <summary>以 (radius, angle) 为中心摆一个 size 大小的标记（径向 u / 切向 v 两个轴）。</summary>
	private static void Marker(SigilShape s, float radius, float angle, MarkerShape shape, float size, float z)
	{
		float ux = MathF.Cos(angle);
		float uy = MathF.Sin(angle);
		float vx = -uy;
		float vy = ux;
		float cx = ux * radius;
		float cy = uy * radius;
		float h = MathF.Max(size, 0.5f) * 0.5f;
		if (shape == MarkerShape.Dot)
		{
			h *= 0.6f;
		}
		switch (shape)
		{
		case MarkerShape.Cross:
			AddCartesian(s, cx + ux * h, cy + uy * h, cx - ux * h, cy - uy * h, z);
			AddCartesian(s, cx + vx * h, cy + vy * h, cx - vx * h, cy - vy * h, z);
			break;
		case MarkerShape.Square:
		{
			float p1x = cx + ux * h + vx * h;
			float p1y = cy + uy * h + vy * h;
			float p2x = cx - ux * h + vx * h;
			float p2y = cy - uy * h + vy * h;
			float p3x = cx - ux * h - vx * h;
			float p3y = cy - uy * h - vy * h;
			float p4x = cx + ux * h - vx * h;
			float p4y = cy + uy * h - vy * h;
			AddCartesian(s, p1x, p1y, p2x, p2y, z);
			AddCartesian(s, p2x, p2y, p3x, p3y, z);
			AddCartesian(s, p3x, p3y, p4x, p4y, z);
			AddCartesian(s, p4x, p4y, p1x, p1y, z);
			break;
		}
		case MarkerShape.Triangle:
		{
			float ox = cx + ux * h;
			float oy = cy + uy * h;
			float lx = cx - ux * h + vx * h;
			float ly = cy - uy * h + vy * h;
			float rx = cx - ux * h - vx * h;
			float ry = cy - uy * h - vy * h;
			AddCartesian(s, ox, oy, lx, ly, z);
			AddCartesian(s, lx, ly, rx, ry, z);
			AddCartesian(s, rx, ry, ox, oy, z);
			break;
		}
		default:
		{
			float ox = cx + ux * h;
			float oy = cy + uy * h;
			float sx = cx + vx * h;
			float sy = cy + vy * h;
			float ix = cx - ux * h;
			float iy = cy - uy * h;
			float tx = cx - vx * h;
			float ty = cy - vy * h;
			AddCartesian(s, ox, oy, sx, sy, z);
			AddCartesian(s, sx, sy, ix, iy, z);
			AddCartesian(s, ix, iy, tx, ty, z);
			AddCartesian(s, tx, ty, ox, oy, z);
			break;
		}
		}
	}
}
