#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>浮空法阵（多层圆盘）观感参数（config 映射）。</summary>
public sealed class SigilDomeOptions
{
	/// <summary>锚线螺旋量（弧度）：0 = 笔直，0.5~0.8 = 优雅的扭转。</summary>
	public float Twist { get; set; } = 0.55f;

	/// <summary>顶盘核心徽记。</summary>
	public bool Crown { get; set; } = true;

	/// <summary>底盘外缘的立式符文带 + 外唇（图 2「外圈符文环带」→ 盘看起来有厚度）。</summary>
	public bool Rim { get; set; } = true;

	/// <summary>圆盘层数（含底盘）：3 = 底/中/顶，2 = 底/中。</summary>
	public int Discs { get; set; } = 3;

	/// <summary>空间锚定斜线数量（从底盘边缘斜拉向高空）。</summary>
	public int Anchors { get; set; } = 8;

	/// <summary>底盘内部的徽记风格：`none` = 只有轮盘骨架（默认，最接近参考图）；也可 arcane/ward/astrolabe/sun。</summary>
	public string Style { get; set; } = "none";

	/// <summary>4 个倾斜侧阵门（图 1/2/3 的东/西/南/北侧阵门）。</summary>
	public bool Gates { get; set; } = true;

	/// <summary>侧阵门倾角（度）：0 = 水平盘，90 = 竖直（法线朝外上方）。</summary>
	public float GateTilt { get; set; } = 68f;

	/// <summary>侧阵门半径系数（× 法阵半径）。</summary>
	public float GateSize { get; set; } = 0.22f;

	/// <summary>环形轨道带 + 弧形连接臂（图 2「弧形轨道 / 弧形连接轨道」）。</summary>
	public bool Tracks { get; set; } = true;

	/// <summary>中心主轴 + 立体核心 + 贯通轴线（图 3「主轴连接层」/ 图 1 底盘中心核心）。</summary>
	public bool Axis { get; set; } = true;

	/// <summary>自转图元的更新间隔（tick）：2 = 隔一 tick（省一半每 tick 网络），1 = 每 tick。</summary>
	public int AnimationStride { get; set; } = 2;
}

/// <summary>
/// 超位魔法「坠落天空」的**三维浮空装置法阵**（结构参考三张"浮空装置"设计图）：
///
/// <para>沿中心竖直轴堆叠的**多层同轴圆盘**（每个 = <c>外三环 + 环间连续铭文带 + 内环 + 轮辐 + 中心圆</c>，
/// 底盘最大、中盘 0.70R 带花型徽记、顶盘 0.42R 带核心徽记），再加装置化部件：
/// **外缘立式符文带**（外唇，图 2「外圈符文环带」）、**分段环形轨道 + 弧形连接臂**（图 2「弧形连接轨道」）、
/// **4 个倾斜侧阵门**（图 1/2/3 东/西/南/北侧阵门：独立倾斜小盘，自转发生在门平面内）、
/// **中心主轴 + 立体核心 + 贯通轴线**（图 3「主轴连接层」「东西南北轴」）；最后是
/// **竖直支撑柱**、**斜向锚定线**与**顶盘向上的能量柱**。</para>
///
/// <para>动画：每个部件有自己的 <c>SpawnTime</c>（自下而上 + 先盘后锚），到点后在 <c>partGrow</c> 内
/// ease-out 从 0.01 放大到 1（建好即缩成一个点）；倒数末尾 <see cref="SetPulse"/> 用整体加宽做发动信号。</para>
/// </summary>
public sealed class SigilDome : IBeamGroup
{
	private sealed class Part
	{
		public float Spawn;
		public float Grow = 1f;
		public bool Built;
		public Func<List<SigilShape>>? Factory;
		public readonly SigilShapeGroup Group = new SigilShapeGroup();
	}

	private readonly List<Part> _parts = new List<Part>();
	private int _plannedBeams;
	private readonly Vector _origin;
	private readonly float _radius;
	private readonly SigilPalette _palette;
	private readonly float _width;
	private readonly float _spin;
	private readonly float _growSeconds;
	private readonly float _startTime;
	private readonly float _twist;
	private readonly bool _crownEnabled;
	private readonly bool _rimEnabled;
	private readonly bool _gatesEnabled;
	private readonly float _gateTilt;
	private readonly float _gateSize;
	private readonly bool _tracksEnabled;
	private readonly bool _axisEnabled;
	private readonly int _discs;
	private readonly int _anchors;
	private readonly SigilStyle _style;
	private readonly bool _hasEmblem;
	private readonly int _stride;
	private float _appliedPulse = -1f;

	public int BeamCount => _plannedBeams;

	public SigilDome(Vector center, float radius, SigilPalette palette, float width,
		float spin, float growSeconds, float startTime, SigilDomeOptions options)
	{
		_origin = new Vector(center.X, center.Y, center.Z);
		_radius = MathF.Max(radius, 24f);
		_palette = palette;
		_width = MathF.Max(width, 0.5f);
		_spin = spin;
		_growSeconds = MathF.Max(growSeconds, 0.4f);
		_startTime = startTime;
		_twist = Math.Clamp(options.Twist, -1.6f, 1.6f);
		_crownEnabled = options.Crown;
		_rimEnabled = options.Rim;
		_gatesEnabled = options.Gates;
		_gateTilt = Math.Clamp(options.GateTilt, 0f, 90f) * MathF.PI / 180f;
		_gateSize = Math.Clamp(options.GateSize, 0.06f, 0.45f);
		_tracksEnabled = options.Tracks;
		_axisEnabled = options.Axis;
		_discs = Math.Clamp(options.Discs, 1, 4);
		_anchors = Math.Clamp(options.Anchors, 0, 16);
		_style = SigilStyles.Parse(options.Style);
		_stride = Math.Clamp(options.AnimationStride, 1, 4);
		string styleName = (options.Style ?? string.Empty).Trim().ToLowerInvariant();
		_hasEmblem = styleName.Length > 0 && styleName != "none" && styleName != "off";

		float partGrow = MathF.Max(_growSeconds * 0.45f, 0.25f);
		float r = _radius;

		// ★ 所有部件都只登记"蓝图"（lambda），到 SpawnTime 才真正建 CBeam：
		// 否则按 E 那一帧会一次性铺出上千根光束 —— 客户端实体槽被快速复用/实体表错位，
		// 之后引擎会以 CopyExistingEntity: missing client entity 致命错误自杀（已实测的崩溃类）。
		// ① 底盘：轮盘骨架（外三环 + 铭文带 + 内环 + 轮辐 + 中心圆）＋ 可选徽记。
		AddPart(0f, partGrow, delegate
		{
			List<SigilShape> baseDisc = DialShell(r, 2f, 16, 3301);
			if (_hasEmblem)
			{
				SigilParams p = new SigilParams { Density = 1f };
				baseDisc.AddRange(SigilStyles.Build(_style, _origin, r * 0.84f, p, palette, _width * 0.8f, _spin * 0.7f, 0.30f, true));
			}
			return baseDisc;
		});

		// ①b 外缘：立式符文带 + 外唇（让底盘看起来有厚度）。
		if (_rimEnabled)
		{
			AddPart(_growSeconds * 0.04f, partGrow, () => BuildRim(r));
		}

		// ② 环形轨道带 + 贯通轴线（装置底座）。
		if (_tracksEnabled)
		{
			AddPart(_growSeconds * 0.10f, partGrow, () => BuildTracks(r));
		}

		// ③ 侧阵门：4 个倾斜独立小盘 + 弧形连接臂（图上的东/西/南/北侧阵门）。
		if (_gatesEnabled)
		{
			AddPart(_growSeconds * 0.16f, partGrow, () => BuildGates(r));
		}

		// ④ 中心主轴 + 立体核心（底盘中心的多面体核心，图 1）。
		if (_axisEnabled)
		{
			AddPart(_growSeconds * 0.28f, partGrow, () => BuildAxis(r));
		}

		// ⑤ 中盘：轮盘 + 花型徽记（横向传导魔力的那一层）。
		if (_discs >= 2)
		{
			AddPart(_growSeconds * 0.42f, partGrow, delegate
			{
				List<SigilShape> mid = DialShell(r * 0.70f, r * 0.62f, 12, 3307);
				mid.AddRange(BuildFlowerEmblem(r * 0.70f * 0.56f, r * 0.62f + 4.4f));
				return mid;
			});
		}

		// ⑥ 顶盘：轮盘 + 核心徽记（能量汇聚点）。
		if (_discs >= 3 && _crownEnabled)
		{
			AddPart(_growSeconds * 0.62f, partGrow, delegate
			{
				List<SigilShape> top = DialShell(r * 0.42f, r * 1.10f, 8, 3313);
				top.AddRange(BuildCoreEmblem(r * 0.42f * 0.62f, r * 1.10f + 4.4f));
				return top;
			});
		}

		// ⑦ 支撑柱 + 空间锚定斜线：把多层圆盘连起来、并钉在空间里。
		if (_anchors > 0)
		{
			AddPart(_growSeconds * 0.80f, partGrow, () => BuildAnchors(r));
		}

		// ⑧ 能量柱：顶盘中心向上，通往天空。
		AddPart(_growSeconds * 0.95f, partGrow, () => BuildSpire(r));
	}

	/// <summary>
	/// 一个"轮盘"：外三环 + 环间连续铭文带 + 两圈内环 + 从内环射到外环的轮辐 + 中心小圆。
	/// 半径越小段数越少（离得远看不清，别浪费实体）。
	/// </summary>
	private List<SigilShape> DialShell(float r, float height, int spokes, int seed)
	{
		int seg = Math.Clamp((int)(r / 14f), 12, 44);
		int inner = Math.Clamp((int)(r / 30f), 8, 22);
		return new List<SigilShape>
		{
			// 外圈三环（外粗内细）+ 环间的铭文带 → 参考图里那圈"连续符文"。
			SigilBuilder.Arc(_origin, r, seg, height, _palette.Core, _palette.Halo, _width, true),
			SigilBuilder.Arc(_origin, r * 0.955f, seg, height + 1.2f, _palette.Mid, _palette.Halo, _width * 0.75f, false),
			SigilBuilder.Arc(_origin, r * 0.905f, seg, height + 2.0f, _palette.Mid, _palette.Halo, _width * 0.7f, false),
			SigilBuilder.RuneBand(_origin, r * 0.905f, r * 0.998f, Math.Clamp((int)(r / 9f), 16, 90), height + 2.8f, _spin * 0.7f, seed, _palette.Core, _palette.Halo, _width * 0.8f, false),
			// 盘面：中环 + 内环 + 中心小圆。
			SigilBuilder.Arc(_origin, r * 0.62f, inner + 8, height + 3.4f, _palette.Mid, _palette.Halo, _width * 0.7f, false),
			SigilBuilder.Arc(_origin, r * 0.36f, inner, height + 4.2f, _palette.Core, _palette.Halo, _width * 0.7f, false),
			SigilBuilder.Arc(_origin, r * 0.12f, 12, height + 5.0f, _palette.Mid, _palette.Halo, _width * 0.6f, false),
			// 轮辐：从内环射到外环（参考图的盘面就是靠这个撑起来的）。
			SigilBuilder.Spokes(_origin, r * 0.38f, r * 0.93f, SigilBuilder.StarAngles(spokes), height + 5.6f, _spin * 0.45f, _palette.Mid, _palette.Halo, _width * 0.7f, false)
		};
	}

	/// <summary>花型徽记：两层交错的尖角星 + 一圈小圆（撑出"花瓣/卷草"的密度感）。</summary>
	private List<SigilShape> BuildFlowerEmblem(float r, float z)
	{
		return new List<SigilShape>
		{
			SigilBuilder.CompassStar(_origin, r, r * 0.34f, 12, z, _spin * 1.1f, _palette.Core, _palette.Halo, _width * 0.8f, true),
			SigilBuilder.CompassStar(_origin, r * 0.62f, r * 0.20f, 6, z + 1.4f, -_spin * 1.5f, _palette.Mid, _palette.Halo, _width * 0.7f, false, phase: MathF.PI / 6f),
			SigilBuilder.Epicycle(_origin, r * 0.72f, 6, r * 0.16f, 8, z + 2.6f, 0f, _palette.Mid, _palette.Halo, _width * 0.6f, false)
		};
	}

	/// <summary>顶层核心徽记：小尖角星 + 内环。</summary>
	private List<SigilShape> BuildCoreEmblem(float r, float z)
	{
		return new List<SigilShape>
		{
			SigilBuilder.CompassStar(_origin, r, r * 0.30f, 8, z, _spin * 1.2f, _palette.Core, _palette.Halo, _width * 0.8f, true),
			SigilBuilder.Arc(_origin, r * 0.46f, 12, z + 1.4f, _palette.Mid, _palette.Halo, _width * 0.6f, false)
		};
	}

	/// <summary>底盘外缘的「外圈符文环带」（图 2）：外唇线 + 立式短符文 + 外唇顶环 → 盘看起来有厚度。</summary>
	private List<SigilShape> BuildRim(float r)
	{
		int seg = Math.Clamp((int)(r / 12f), 16, 56);
		float lip = 2f + r * 0.010f;
		return new List<SigilShape>
		{
			SigilBuilder.Arc(_origin, r * 1.002f, seg, lip, _palette.Mid, _palette.Halo, _width * 0.8f, false),
			SigilBuilder.RimTicks(_origin, r * 1.004f, Math.Clamp((int)(r / 13f), 20, 72), lip, r * 0.055f, 4409, _palette.Core, _palette.Halo, _width * 0.8f, false),
			SigilBuilder.Arc(_origin, r * 1.020f, seg, lip + r * 0.044f, _palette.Halo, _palette.Halo, _width * 0.7f, false)
		};
	}

	/// <summary>环形轨道带（4 段 60°，段中心对准侧阵门）+ 每段两根弧形连接臂（图 2「弧形连接轨道」）。</summary>
	private List<SigilShape> BuildTracks(float r)
	{
		List<SigilShape> shapes = new List<SigilShape>();
		float railR = r * 1.06f;
		float railZ = r * 0.30f;
		float gateD = r * 1.14f;
		float gateZ = r * 0.34f;
		float half = MathF.PI / 6f;
		int seg = Math.Clamp((int)(r / 44f), 6, 12);
		for (int i = 0; i < 4; i++)
		{
			float a = i * MathF.PI * 0.5f;
			shapes.Add(SigilBuilder.ArcSpan(_origin, railR, a - half, a + half, seg, railZ, 0f, _palette.Mid, _palette.Halo, _width, false));
			shapes.Add(SigilBuilder.ArcTrack(_origin, a, railR, railZ, a, gateD, gateZ + r * 0.09f, r * 0.03f, 8, 0f, _palette.Mid, _palette.Halo, _width * 0.7f, false));
			shapes.Add(SigilBuilder.ArcTrack(_origin, a, railR, railZ, a, gateD, gateZ - r * 0.09f, r * 0.03f, 8, 0f, _palette.Core, _palette.Halo, _width * 0.6f, false));
		}
		return shapes;
	}

	/// <summary>
	/// 4 个**倾斜侧阵门**（图 1/2/3 的东/西/南/北侧阵门）：独立小盘 =
	/// 外环（带光晕）+ 次环 + 内环 + 中心小环 + 背面厚度环 + 面内辐条（自转）+ 中心十字。
	/// </summary>
	private List<SigilShape> BuildGates(float r)
	{
		List<SigilShape> shapes = new List<SigilShape>();
		float d = r * 1.14f;
		float z = r * 0.34f;
		float rg = r * _gateSize;
		float tilt = _gateTilt;
		for (int i = 0; i < 4; i++)
		{
			float a = i * MathF.PI * 0.5f;
			shapes.Add(SigilBuilder.TiltedRing(_origin, d, a, z, rg, 16, tilt, 0f, 0f, _palette.Core, _palette.Halo, _width, true));
			shapes.Add(SigilBuilder.TiltedRing(_origin, d, a, z, rg * 0.86f, 13, tilt, 0f, 0f, _palette.Mid, _palette.Halo, _width * 0.75f, false));
			shapes.Add(SigilBuilder.TiltedRing(_origin, d, a, z, rg * 0.55f, 11, tilt, 0f, 0f, _palette.Mid, _palette.Halo, _width * 0.7f, false));
			shapes.Add(SigilBuilder.TiltedRing(_origin, d, a, z, rg * 0.16f, 7, tilt, 0f, 0f, _palette.Core, _palette.Halo, _width * 0.6f, false));
			shapes.Add(SigilBuilder.TiltedRing(_origin, d, a, z, rg, 13, tilt, -rg * 0.13f, 0f, _palette.Halo, _palette.Halo, _width * 0.7f, false));
			shapes.Add(SigilBuilder.TiltedSpokes(_origin, d, a, z, rg * 0.18f, rg * 0.94f, 8, tilt, 0f, _spin * 0.9f, _palette.Core, _palette.Halo, _width * 0.6f, false));
			shapes.Add(SigilBuilder.TiltedSpokes(_origin, d, a, z, 0f, rg * 0.15f, 4, tilt, 0f, 0f, _palette.Core, _palette.Halo, _width * 0.6f, false));
		}
		return shapes;
	}

	/// <summary>
	/// 中心主轴（贯穿底盘→顶盘）+ 立体核心（底盘中心六棱台，图 1）+ 贯通轴线（图 3「东西南北轴」）。
	/// </summary>
	private List<SigilShape> BuildAxis(float r)
	{
		float z0 = 2f;
		float zTop = _discs >= 3 ? r * 1.10f : (_discs >= 2 ? r * 0.62f : r * 0.40f);
		float hexBase = r * 0.055f;
		float hexTop = r * 0.038f;
		float hexZ = z0 + r * 0.075f;
		List<SigilShape> shapes = new List<SigilShape>
		{
			// 主轴：4 根细竖线绕中心小圆（"轴"的轮廓）。
			SigilBuilder.Spokes3D(_origin, r * 0.022f, r * 0.022f, z0, zTop, SigilBuilder.StarAngles(4), 0f, _palette.Core, _palette.Halo, _width * 0.8f, true),
			// 轴承环（底盘面）。
			SigilBuilder.Arc(_origin, r * 0.05f, 12, z0 + 1.2f, _palette.Mid, _palette.Halo, _width * 0.6f, false),
			// 立体核心：底六边形 + 顶六边形 + 6 根竖棱 + 顶部小尖。
			SigilBuilder.Polygon(_origin, hexBase, 6, z0 + 0.6f, 0f, _palette.Core, _palette.Halo, _width * 0.8f, true),
			SigilBuilder.Polygon(_origin, hexTop, 6, hexZ, 0f, _palette.Mid, _palette.Halo, _width * 0.7f, false),
			SigilBuilder.Spokes3D(_origin, hexBase, hexTop, z0 + 0.6f, hexZ, SigilBuilder.StarAngles(6), 0f, _palette.Core, _palette.Halo, _width * 0.7f, false),
			SigilBuilder.Spokes3D(_origin, 0f, hexTop * 0.9f, hexZ + r * 0.02f, hexZ + r * 0.05f, SigilBuilder.StarAngles(4), 0f, _palette.Core, _palette.Halo, _width * 0.6f, false),
			// 贯通轴线：4 根从中心射到外缘。
			SigilBuilder.Spokes(_origin, hexBase, r * 0.96f, SigilBuilder.StarAngles(4), z0 + 2.6f, 0f, _palette.Mid, _palette.Halo, _width * 0.6f, false)
		};
		if (_discs >= 2)
		{
			shapes.Add(SigilBuilder.Arc(_origin, r * 0.05f, 12, r * 0.62f + 1.2f, _palette.Mid, _palette.Halo, _width * 0.6f, false));
		}
		return shapes;
	}

	/// <summary>支撑柱（把圆盘连成一台"装置"）+ 空间锚定斜线（斜拉向高空）。</summary>
	private List<SigilShape> BuildAnchors(float r)
	{
		List<SigilShape> shapes = new List<SigilShape>();
		float[] anchors = SigilBuilder.StarAngles(_anchors);
		// 斜向锚线：底盘边缘 → 高空外扩（带 twist 的螺旋感）。
		shapes.Add(SigilBuilder.Spokes3D(_origin, r * 0.98f, r * 1.95f, 0f, r * 1.45f, anchors, 0f, _palette.Mid, _palette.Halo, _width * 0.7f, false));
		shapes.Add(SigilBuilder.Spokes3D(_origin, r * 0.98f, r * 1.45f, 0f, r * 0.95f, anchors, 0f, _palette.Core, _palette.Halo, _width * 0.6f, false, phase: _twist));
		// 支撑柱：底盘边缘 → 中盘边缘（→ 顶盘），4 根，错开锚线角度。
		float[] posts = SigilBuilder.StarAngles(4, MathF.PI * 0.25f);
		float hMid = _discs >= 2 ? r * 0.62f : r * 0.55f;
		shapes.Add(SigilBuilder.Spokes3D(_origin, r * 0.96f, r * 0.66f, 0f, hMid, posts, 0f, _palette.Core, _palette.Halo, _width * 0.9f, true));
		if (_discs >= 3)
		{
			shapes.Add(SigilBuilder.Spokes3D(_origin, r * 0.66f, r * 0.38f, hMid, r * 1.08f, posts, 0f, _palette.Core, _palette.Halo, _width * 0.8f, true));
		}
		return shapes;
	}

	/// <summary>能量柱：顶盘中心 → 更高处的细竖柱（通往天空的"通道"）。</summary>
	private List<SigilShape> BuildSpire(float r)
	{
		float z = _discs >= 3 ? r * 1.10f : (_discs >= 2 ? r * 0.62f : r * 0.55f);
		return new List<SigilShape>
		{
			SigilBuilder.Spokes3D(_origin, 0f, 0f, z, z + r * 1.9f, new float[] { 0f }, 0f, _palette.Core, _palette.Halo, _width * 1.1f, true)
		};
	}

	private void AddPart(float spawn, float grow, Func<List<SigilShape>> factory)
	{
		if (factory == null)
		{
			return;
		}
		Part part = new Part { Spawn = spawn, Grow = MathF.Max(grow, 0.05f), Factory = factory };
		part.Group.Stride = _stride;
		_parts.Add(part);
		// 计划束数：用"计数模式"跑一遍蓝图（不建实体），供 rtddome 之类的回显使用。
		_plannedBeams += CountBeams(factory);
	}

	/// <summary>到点才把蓝图变成实体（分帧铺开，避免单帧上千 CBeam）。</summary>
	private static void Build(Part part)
	{
		part.Built = true;
		if (part.Factory == null)
		{
			return;
		}
		part.Group.AddRange(part.Factory());
		part.Factory = null;
	}

	private static int CountBeams(Func<List<SigilShape>> factory)
	{
		bool previous = SigilShape.CountingOnly;
		SigilShape.CountingOnly = true;
		try
		{
			int total = 0;
			foreach (SigilShape shape in factory())
			{
				total += shape.BeamCount;
			}
			return total;
		}
		catch
		{
			return 0;
		}
		finally
		{
			SigilShape.CountingOnly = previous;
		}
	}

	public void Update(Vector center, float now, float scale = 1f)
	{
		if (scale <= 0f)
		{
			scale = 0.01f;
		}
		float age = now - _startTime;
		foreach (Part part in _parts)
		{
			float t = (age - part.Spawn) / part.Grow;
			if (t <= 0f)
			{
				continue;
			}
			if (!part.Built)
			{
				Build(part);
			}
			float grow = EaseOut(Math.Clamp(t, 0f, 1f));
			part.Group.Update(center, now, MathF.Max(scale * grow, 0.01f));
		}
	}

	/// <summary>发动信号：整体加宽（0~1）。</summary>
	public void SetPulse(float pulse)
	{
		pulse = Math.Clamp(pulse, 0f, 1f);
		if (MathF.Abs(pulse - _appliedPulse) < 0.08f)
		{
			return;
		}
		_appliedPulse = pulse;
		float scale = 1f + pulse * 1.6f;
		foreach (Part part in _parts)
		{
			foreach (SigilShape shape in part.Group.Shapes)
			{
				shape.SetWidthScale(scale);
			}
		}
	}

	public void Remove()
	{
		foreach (Part part in _parts)
		{
			part.Group.Remove();
		}
		_parts.Clear();
		_plannedBeams = 0;
	}

	public int RemoveChunk(int budget)
	{
		int removed = 0;
		for (int i = _parts.Count - 1; i >= 0 && removed < budget; i--)
		{
			removed += _parts[i].Group.RemoveChunk(Math.Max(budget - removed, 1));
		}
		return removed;
	}

	public bool IsEmpty
	{
		get
		{
			foreach (Part part in _parts)
			{
				if (!part.Group.IsEmpty)
				{
					return false;
				}
			}
			return true;
		}
	}

	private static float EaseOut(float t)
	{
		return 1f - (1f - t) * (1f - t) * (1f - t);
	}
}
