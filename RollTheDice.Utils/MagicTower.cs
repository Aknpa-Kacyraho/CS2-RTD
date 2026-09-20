#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// "超位魔法"式的**竖向法阵塔**：把一圈圈<see cref="MagicCircle"/>（外环 + 辐条）从下往上叠成塔，
/// 逐层抬升、正反交替旋转、整体可收缩。刻意做成**水平、规整、层层同轴**，
/// 不会出现离地单环那种"一端地一端天"的杂乱斜线。
///
/// 设计口径：够高（不被地形遮挡）、够大（远处看得清）、够多（默认 8 层往上）、够炫（双色交替）。
/// 全部是服务器端 CBeam，无素材依赖（见 docs/research/2026-09-20-cs2-particle-system.md）。
/// </summary>
public sealed class MagicTower
{
	private sealed class Ring
	{
		public MagicCircle Circle = null!;
		public float Radius;
		public float Outer;
		public float Height;
		public float Spin;
	}

	private readonly List<Ring> _rings = new List<Ring>();

	public MagicTower(Vector center, Color colorA, Color colorB, float width, int segments, int spokes, int count, float heightBase, float heightStep, float radius, float outerRadius, float radiusDecay, float spin)
	{
		count = Math.Clamp(count, 1, 16);
		radiusDecay = Math.Clamp(radiusDecay, 0f, 0.6f);
		float r = radius;
		float o = outerRadius;
		for (int i = 0; i < count; i++)
		{
			float h = heightBase + heightStep * i;
			Color color = (i % 2 == 0) ? colorA : colorB;
			MagicCircle circle = new MagicCircle(new Vector(center.X, center.Y, center.Z + h), r, o, color, width, segments, spokes, 0f);
			float ringSpin = spin * ((i % 2 == 0) ? 1f : -1f) / (1f + i * 0.08f);
			_rings.Add(new Ring { Circle = circle, Radius = r, Outer = o, Height = h, Spin = ringSpin });
			r *= 1f - radiusDecay;
			o *= 1f - radiusDecay;
		}
	}

	/// <summary>每 tick 重新定位底座、旋转并整体缩放（<paramref name="scale"/> 用于倒数收缩）。</summary>
	public void Update(Vector center, float time, float scale = 1f)
	{
		if (scale <= 0f)
		{
			scale = 0.01f;
		}
		foreach (Ring ring in _rings)
		{
			ring.Circle.SetRadius(ring.Radius * scale, ring.Outer * scale);
			ring.Circle.Update(new Vector(center.X, center.Y, center.Z + ring.Height), time * ring.Spin);
		}
	}

	public void Remove()
	{
		foreach (Ring ring in _rings)
		{
			ring.Circle.Remove();
		}
		_rings.Clear();
	}
}
