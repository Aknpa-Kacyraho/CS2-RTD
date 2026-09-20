#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 用 <c>CBeam</c> 画一个**水平铺在地面**的魔法阵（圆环 + 辐条 + 外环）。
///
/// 为什么不用现成 vpcf：CS2 里粒子的尺寸/朝向由 vpcf 作者决定，服务器无法通用放大或转向
/// （见 docs/research/2026-09-20-cs2-particle-system.md）。而 CBeam 的起终点完全由服务器控制，
/// 所以大小 / 水平朝向 / 位置 / 跟随 都能精确掌握，且无需任何自制素材。
/// </summary>
public sealed class MagicCircle
{
	private readonly struct RingSeg
	{
		public readonly CBeam Beam;
		public readonly float AngleA;
		public readonly float AngleB;
		public RingSeg(CBeam beam, float a, float b) { Beam = beam; AngleA = a; AngleB = b; }
	}

	private readonly struct SpokeSeg
	{
		public readonly CBeam Beam;
		public readonly float Angle;
		public SpokeSeg(CBeam beam, float a) { Beam = beam; Angle = a; }
	}

	private readonly List<RingSeg> _ring = new List<RingSeg>();
	private readonly List<SpokeSeg> _spokes = new List<SpokeSeg>();
	private readonly List<RingSeg> _outer = new List<RingSeg>();
	private readonly float _zOffset;
	private readonly float _innerFrac;

	private float _radius;
	private float _outerRadius;
	private Vector _center;

	public MagicCircle(Vector center, float radius, float outerRadius, Color color, float width, int segments, int spokes, float zOffset = 4f, float innerFrac = 0.42f)
	{
		_center = center;
		_radius = radius;
		_outerRadius = outerRadius;
		_zOffset = zOffset;
		_innerFrac = innerFrac;
		segments = Math.Clamp(segments, 3, 64);
		spokes = Math.Clamp(spokes, 0, 32);

		for (int i = 0; i < segments; i++)
		{
			float a = i * 2f * MathF.PI / segments;
			float b = (i + 1) * 2f * MathF.PI / segments;
			_ring.Add(new RingSeg(CreateBeam(color, width), a, b));
			if (outerRadius > 0f)
			{
				_outer.Add(new RingSeg(CreateBeam(color, width), a, b));
			}
		}
		for (int i = 0; i < spokes; i++)
		{
			float a = i * 2f * MathF.PI / MathF.Max(spokes, 1);
			_spokes.Add(new SpokeSeg(CreateBeam(color, width), a));
		}
		Update(center, 0f);
	}

	/// <summary>重新定位（可每 tick 调用实现跟随）并旋转。</summary>
	public void Update(Vector center, float rotation)
	{
		_center = center;
		foreach (RingSeg seg in _ring)
		{
			Place(seg.Beam, seg.AngleA, seg.AngleB, _radius, rotation, _zOffset);
		}
		foreach (RingSeg seg in _outer)
		{
			Place(seg.Beam, seg.AngleA, seg.AngleB, _outerRadius, rotation + 0.35f, _zOffset + 6f);
		}
		float inner = _radius * _innerFrac;
		foreach (SpokeSeg seg in _spokes)
		{
			Vector p1 = Point(seg.Angle + rotation, inner, _zOffset);
			Vector p2 = Point(seg.Angle + rotation, _radius * 0.98f, _zOffset + 6f);
			Move(seg.Beam, p1, p2);
		}
	}

	/// <summary>改半径（用于倒数收缩环）。</summary>
	public void SetRadius(float radius, float outerRadius)
	{
		_radius = radius;
		_outerRadius = outerRadius;
	}

	public void Remove()
	{
		foreach (RingSeg seg in _ring) Kill(seg.Beam);
		foreach (RingSeg seg in _outer) Kill(seg.Beam);
		foreach (SpokeSeg seg in _spokes) Kill(seg.Beam);
	}

	private void Place(CBeam beam, float a, float b, float radius, float rotation, float z)
	{
		Move(beam, Point(a + rotation, radius, z), Point(b + rotation, radius, z));
	}

	private Vector Point(float angle, float radius, float z)
	{
		return new Vector(_center.X + MathF.Cos(angle) * radius, _center.Y + MathF.Sin(angle) * radius, _center.Z + z);
	}

	private static CBeam CreateBeam(Color color, float width)
	{
		return BeamFx.Create(color, width)!;
	}

	private static void Move(CBeam beam, Vector start, Vector end)
	{
		BeamFx.Move(beam, start, end);
	}

	private static void Kill(CBeam beam)
	{
		BeamFx.Kill(beam);
	}
}
