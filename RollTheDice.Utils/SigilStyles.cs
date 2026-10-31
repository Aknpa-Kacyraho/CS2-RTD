#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 法阵刻画风格（参考图）：<see cref="Arcane"/> = 现有奥术法阵，
/// <see cref="Ward"/> = 方阵封印，<see cref="Astrolabe"/> = 星盘，<see cref="Sun"/> = 太阳。
/// </summary>
public enum SigilStyle
{
	Arcane = 0,
	Ward = 1,
	Astrolabe = 2,
	Sun = 3
}

/// <summary>
/// 风格库：按风格把一组刻画（<see cref="SigilShape"/>）装出来。
///
/// <para>半径分数是内部组合常量（描述"长什么样"），要素数量走 <see cref="SigilParams"/>（密度/条数），
/// <c>detail</c> 是整体降采样系数——天空法阵群那种"层多、离得远"的场合用它把每层压到几十根束。</para>
///
/// <para>所有 builder 在 <c>spin == 0</c> 时产出静态图元（容器跳过每 tick 更新），
/// 所以"不转的层"几乎不花每 tick 成本。</para>
/// </summary>
public static class SigilStyles
{
	/// <summary>mixed 模式下的轮换池。</summary>
	private static readonly SigilStyle[] MixedPool = { SigilStyle.Arcane, SigilStyle.Ward, SigilStyle.Astrolabe, SigilStyle.Sun };

	public static SigilStyle Parse(string? name)
	{
		switch ((name ?? string.Empty).Trim().ToLowerInvariant())
		{
		case "ward":
		case "square":
		case "seal":
			return SigilStyle.Ward;
		case "astrolabe":
		case "orrery":
		case "rings":
			return SigilStyle.Astrolabe;
		case "sun":
		case "solar":
			return SigilStyle.Sun;
		default:
			return SigilStyle.Arcane;
		}
	}

	public static bool IsMixed(string? name)
	{
		return string.IsNullOrWhiteSpace(name) || name.Trim().Equals("mixed", StringComparison.OrdinalIgnoreCase);
	}

	public static IReadOnlyList<SigilStyle> Pool(string? name)
	{
		return IsMixed(name) ? MixedPool : new SigilStyle[] { Parse(name) };
	}

	/// <summary>按风格装配一组刻画（中心点 / 半径 / 密度参数 / 配色 / 线宽 / 自转 / 降采样 / 是否允许光晕）。</summary>
	public static List<SigilShape> Build(SigilStyle style, Vector center, float radius, SigilParams p, SigilPalette pal, float width, float spin, float detail, bool halo)
	{
		detail = Math.Clamp(detail, 0.2f, 1.6f);
		List<SigilShape> shapes = new List<SigilShape>();
		switch (style)
		{
		case SigilStyle.Ward:
			BuildWard(shapes, center, radius, p, pal, width, spin, detail, halo);
			break;
		case SigilStyle.Astrolabe:
			BuildAstrolabe(shapes, center, radius, p, pal, width, spin, detail, halo);
			break;
		case SigilStyle.Sun:
			BuildSun(shapes, center, radius, p, pal, width, spin, detail, halo);
			break;
		default:
			BuildArcane(shapes, center, radius, p, pal, width, spin, detail, halo);
			break;
		}
		return shapes;
	}

	/// <summary>要素数量：按密度缩放后再乘降采样系数。</summary>
	private static int Count(SigilParams p, int baseCount, float detail, int min, int max)
	{
		return p.Scaled((int)MathF.Round(baseCount * detail), min, max);
	}

	/// <summary>圆环段数：按半径给（离得远的层不需要那么多段）。</summary>
	private static int Seg(float radius, float detail)
	{
		return Math.Clamp((int)MathF.Round(radius / 14f * detail), 10, 96);
	}

	/// <summary>奥术法阵（原 ArcaneGroundSigil 的 14 项刻画，detail=1 时与原来逐条一致）。</summary>
	private static void BuildArcane(List<SigilShape> o, Vector center, float radius, SigilParams p, SigilPalette pal, float width, float spin, float detail, bool halo)
	{
		bool dl = halo && p.DoubleLine;
		int segOuter = Count(p, 96, detail, 12, 160);
		int segMid = Count(p, 64, detail, 10, 120);
		int segInner = Count(p, 48, detail, 10, 96);
		int segCore = Count(p, 24, detail, 8, 48);

		// 外框：双同心环（粗/细）本身即"双线"观感。
		o.Add(SigilBuilder.Arc(center, radius * 1.000f, segOuter, 4.0f, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.Arc(center, radius * 0.965f, segOuter, 4.8f, pal.Mid, pal.Halo, width * 0.8f, false));
		// 符文带（密）与刻度环。
		o.Add(SigilBuilder.RuneBand(center, radius * 0.800f, radius * 0.930f, Count(p, p.RuneTicks, detail, 8, 400), 5.6f, spin * 0.6f, p.Seed, pal.Mid, pal.Halo, width, false));
		o.Add(SigilBuilder.TickRing(center, radius * 0.760f, Count(p, p.TickRingCount, detail, 6, 256), radius * 0.035f, 6.4f, -spin * 0.4f, pal.Mid, pal.Halo, width, false));
		// 大五芒星 + 其外框五边形 + 从顶点射到外环的辐条（取代均匀辐条）。
		o.Add(SigilBuilder.Star(center, radius * 0.700f, p.StarPoints, p.StarSkip, 7.2f, spin, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.Polygon(center, radius * 0.700f, p.StarPoints, 6.4f, -spin * 0.55f, pal.Mid, pal.Halo, width * 0.8f, false, phase: MathF.PI / MathF.Max(p.StarPoints, 1)));
		o.Add(SigilBuilder.Spokes(center, radius * 0.700f, radius * 0.965f, SigilBuilder.StarAngles(p.StarPoints), 8.0f, spin, pal.Core, pal.Halo, width, dl));
		// 中带。
		o.Add(SigilBuilder.Arc(center, radius * 0.550f, segMid, 4.6f, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Polygon(center, radius * 0.500f, p.PolygonSides, 9.0f, spin * 0.7f, pal.Mid, pal.Halo, width, false));
		// 内带。
		o.Add(SigilBuilder.Arc(center, radius * 0.400f, segInner, 5.0f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Star(center, radius * 0.340f, 6, 2, 9.8f, -spin * 0.85f, pal.Mid, pal.Halo, width * 0.8f, dl));
		o.Add(SigilBuilder.Star(center, radius * 0.220f, p.StarPoints, p.StarSkip, 10.6f, spin * 1.15f, pal.Core, pal.Halo, width, dl));
		// 阵眼。
		o.Add(SigilBuilder.Arc(center, radius * 0.120f, segCore, 5.2f, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Spokes(center, 0f, radius * 0.100f, new float[] { 0f, MathF.PI / 2f, MathF.PI, MathF.PI * 1.5f }, 10.0f, spin * 1.3f, pal.Core, pal.Halo, width, dl));
	}

	/// <summary>方阵封印（图 1）：方框双线 + 四角括号 + 贯通十字长轴 + 八角罗盘星 + 内环刻度带。</summary>
	private static void BuildWard(List<SigilShape> o, Vector center, float radius, SigilParams p, SigilPalette pal, float width, float spin, float detail, bool halo)
	{
		bool dl = halo && p.DoubleLine;
		int side = Count(p, 20, detail, 4, 48);
		int segIn = Seg(radius * 0.55f, detail);
		// 外框：双层方框 + 四角括号 + 边中点小方块。
		o.Add(SigilBuilder.Square(center, radius * 1.000f, side, 4.0f, 0f, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.Square(center, radius * 0.925f, side, 4.8f, 0f, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.CornerBrackets(center, radius * 1.000f, radius * 0.110f, radius * 0.050f, 5.2f, 0f, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.MarkerRing(center, radius * 0.9625f, Count(p, 4, detail, 2, 4), MarkerShape.Square, radius * 0.030f, 6.0f, 0f, pal.Mid, pal.Halo, width * 0.8f, false, phase: MathF.PI * 0.25f));
		// 贯通十字长轴 + 端标；对角短线。
		o.Add(SigilBuilder.Spokes(center, radius * 1.000f, radius * 1.075f, SigilBuilder.StarAngles(Count(p, 4, detail, 2, 4)), 5.6f, 0f, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.MarkerRing(center, radius * 1.075f, Count(p, 4, detail, 2, 4), MarkerShape.Square, radius * 0.036f, 6.4f, 0f, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.Polygon(center, radius * 0.855f, 8, 6.8f, spin * 0.30f, pal.Mid, pal.Halo, width * 0.8f, false, phase: MathF.PI / 8f));
		o.Add(SigilBuilder.Spokes(center, radius * 0.855f, radius * 0.925f, SigilBuilder.StarAngles(Count(p, 4, detail, 2, 4), MathF.PI * 0.25f), 7.2f, 0f, pal.Mid, pal.Halo, width * 0.7f, false));
		// 内层：符文带 + 刻度环 + 双内环。
		o.Add(SigilBuilder.RuneBand(center, radius * 0.620f, radius * 0.760f, Count(p, p.RuneTicks, detail * 0.8f, 8, 400), 7.8f, spin * 0.45f, p.Seed, pal.Mid, pal.Halo, width, false));
		o.Add(SigilBuilder.TickRing(center, radius * 0.580f, Count(p, p.TickRingCount, detail * 0.8f, 6, 256), radius * 0.032f, 8.4f, -spin * 0.35f, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Arc(center, radius * 0.540f, segIn, 5.0f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Arc(center, radius * 0.500f, segIn, 5.6f, pal.Mid, pal.Halo, width * 0.7f, false));
		// 阵眼：八角罗盘星 + 小四角星 + 阵眼环。
		o.Add(SigilBuilder.CompassStar(center, radius * 0.470f, radius * 0.140f, 8, 8.8f, spin, pal.Core, pal.Halo, width, dl));
		if (detail >= 0.3f)
		{
			o.Add(SigilBuilder.CompassStar(center, radius * 0.270f, radius * 0.085f, 4, 9.6f, -spin * 0.8f, pal.Mid, pal.Halo, width * 0.8f, false, phase: MathF.PI * 0.25f));
		}
		o.Add(SigilBuilder.Arc(center, radius * 0.105f, Count(p, 20, detail, 8, 40), 10.2f, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Spokes(center, 0f, radius * 0.085f, SigilBuilder.StarAngles(4), 10.8f, spin * 1.2f, pal.Core, pal.Halo, width, dl));
	}

	/// <summary>星盘（图 2）：多层同心环 + 环上小圆（本轮）+ 菱形/圆点标记 + 月牙弧 + 双符文带 + 长辐条。</summary>
	private static void BuildAstrolabe(List<SigilShape> o, Vector center, float radius, SigilParams p, SigilPalette pal, float width, float spin, float detail, bool halo)
	{
		bool dl = halo && p.DoubleLine;
		// 多层同心环（内层逐层变细）。
		float[] rings = { 1.000f, 0.895f, 0.760f, 0.600f, 0.430f, 0.250f };
		for (int i = 0; i < rings.Length; i++)
		{
			float r = radius * rings[i];
			Color c = (i % 2 == 0) ? pal.Core : pal.Mid;
			o.Add(SigilBuilder.Arc(center, r, Seg(r, detail), 4.0f + i * 0.8f, c, pal.Halo, (i == 0) ? width : width * 0.8f, i == 0 && dl));
		}
		// 环上小圆（本轮）+ 标记。
		o.Add(SigilBuilder.Epicycle(center, radius * 0.827f, Count(p, 4, detail, 2, 8), radius * 0.075f, Count(p, 10, detail, 4, 20), 5.4f, spin * 0.8f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.MarkerRing(center, radius * 0.680f, Count(p, 4, detail, 2, 4), MarkerShape.Diamond, radius * 0.075f, 6.2f, 0f, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.MarkerRing(center, radius * 0.895f, Count(p, 8, detail, 4, 8), MarkerShape.Dot, radius * 0.026f, 6.6f, 0f, pal.Mid, pal.Halo, width * 0.8f, false, phase: MathF.PI / 8f));
		o.Add(SigilBuilder.TickRing(center, radius * 0.955f, Count(p, p.TickRingCount, detail, 8, 200), radius * 0.028f, 7.0f, -spin * 0.4f, pal.Mid, pal.Halo, width * 0.8f, false));
		// 月牙弧（漩涡感）。
		o.Add(SigilBuilder.ArcSpan(center, radius * 0.515f, 0.25f, 1.05f, Count(p, 12, detail, 3, 40), 7.6f, spin * 1.1f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.ArcSpan(center, radius * 0.515f, 3.39f, 4.19f, Count(p, 12, detail, 3, 40), 7.6f, spin * 1.1f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.ArcSpan(center, radius * 0.330f, 1.60f, 2.30f, Count(p, 9, detail, 3, 30), 8.2f, -spin * 1.25f, pal.Mid, pal.Halo, width * 0.7f, false));
		o.Add(SigilBuilder.ArcSpan(center, radius * 0.330f, 4.75f, 5.45f, Count(p, 9, detail, 3, 30), 8.2f, -spin * 1.25f, pal.Mid, pal.Halo, width * 0.7f, false));
		// 双符文带。
		o.Add(SigilBuilder.RuneBand(center, radius * 0.760f, radius * 0.845f, Count(p, p.RuneTicks, detail * 0.7f, 8, 300), 8.8f, spin * 0.5f, p.Seed + 7, pal.Mid, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.RuneBand(center, radius * 0.290f, radius * 0.370f, Count(p, p.RuneTicks, detail * 0.6f, 8, 300), 9.4f, -spin * 0.65f, p.Seed + 23, pal.Core, pal.Halo, width * 0.7f, false));
		// 长辐条与阵眼。
		o.Add(SigilBuilder.Spokes(center, radius * 0.250f, radius * 0.895f, SigilBuilder.StarAngles(Count(p, 8, detail, 4, 8)), 9.8f, spin * 0.9f, pal.Mid, pal.Halo, width * 0.7f, false));
		o.Add(SigilBuilder.Arc(center, radius * 0.115f, Count(p, 20, detail, 8, 40), 10.2f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Spokes(center, 0f, radius * 0.095f, SigilBuilder.StarAngles(4), 10.6f, spin * 1.3f, pal.Core, pal.Halo, width, dl));
	}

	/// <summary>太阳（图 3）：双外环 + 外扩光芒 + 三角刻度带 + 双三角（六芒轮廓）+ 中心圆。</summary>
	private static void BuildSun(List<SigilShape> o, Vector center, float radius, SigilParams p, SigilPalette pal, float width, float spin, float detail, bool halo)
	{
		bool dl = halo && p.DoubleLine;
		o.Add(SigilBuilder.Arc(center, radius * 1.000f, Seg(radius, detail), 4.0f, pal.Core, pal.Halo, width, dl));
		o.Add(SigilBuilder.Arc(center, radius * 0.940f, Seg(radius * 0.94f, detail), 4.8f, pal.Mid, pal.Halo, width * 0.8f, false));
		// 外扩光芒：伸出本体之外，长短交替（静态，最省）。
		o.Add(SigilBuilder.Corona(center, radius * 1.010f, radius * 1.320f, Count(p, 32, detail, 8, 96), 5.4f, 0f, pal.Mid, pal.Halo, width * 0.7f, false));
		o.Add(SigilBuilder.MarkerRing(center, radius * 0.895f, Count(p, 12, detail, 6, 24), MarkerShape.Triangle, radius * 0.055f, 6.0f, 0f, pal.Core, pal.Halo, width * 0.8f, dl));
		o.Add(SigilBuilder.TickRing(center, radius * 0.845f, Count(p, p.TickRingCount, detail, 8, 200), radius * 0.030f, 6.6f, -spin * 0.35f, pal.Mid, pal.Halo, width * 0.7f, false));
		// 双三角：正向 + 倒置（六芒轮廓）。
		o.Add(SigilBuilder.Polygon(center, radius * 0.720f, 3, 7.4f, spin * 0.50f, pal.Core, pal.Halo, width, dl, phase: MathF.PI * 0.5f));
		o.Add(SigilBuilder.Polygon(center, radius * 0.720f, 3, 8.0f, -spin * 0.50f, pal.Core, pal.Halo, width, dl, phase: MathF.PI * 0.5f + MathF.PI / 3f));
		o.Add(SigilBuilder.Spokes(center, radius * 0.720f, radius * 0.845f, SigilBuilder.StarAngles(3, MathF.PI * 0.5f), 8.6f, 0f, pal.Mid, pal.Halo, width * 0.7f, false));
		o.Add(SigilBuilder.Arc(center, radius * 0.420f, Seg(radius * 0.42f, detail), 9.2f, pal.Core, pal.Halo, width * 0.8f, false));
		o.Add(SigilBuilder.Arc(center, radius * 0.300f, Seg(radius * 0.30f, detail), 9.8f, pal.Mid, pal.Halo, width * 0.7f, false));
		if (detail >= 0.3f)
		{
			o.Add(SigilBuilder.CompassStar(center, radius * 0.230f, radius * 0.070f, 6, 10.4f, spin * 1.1f, pal.Core, pal.Halo, width * 0.8f, dl));
		}
		o.Add(SigilBuilder.Spokes(center, 0f, radius * 0.075f, SigilBuilder.StarAngles(4), 11.0f, spin * 1.4f, pal.Core, pal.Halo, width * 0.8f, false));
	}
}
