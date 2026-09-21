#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 落地冲击环：光柱触及地面后，从落点向四周水平扩散的若干圈 CBeam 环（"地面结晶化冲击波"）。
/// 每圈带一点相位延迟，扩散半径按 ease-out 冲出，结束后由调用方直接 Remove（实体量小，无需分帧）。
/// </summary>
public sealed class ShockRingFx : IBeamGroup
{
	private sealed class Ring
	{
		public MagicCircle Circle = null!;
		public float Delay;
		public int Segments;
	}

	private readonly List<Ring> _rings = new List<Ring>();
	private readonly Vector _center;
	private readonly float _radius;
	private readonly float _seconds;
	private readonly float _startTime;

	public bool Finished { get; private set; }

	public int BeamCount { get; private set; }

	public ShockRingFx(Vector center, int ringCount, float radius, float seconds, Color color, float width, float startTime)
	{
		_center = new Vector(center.X, center.Y, center.Z);
		_radius = MathF.Max(radius, 16f);
		_seconds = MathF.Max(seconds, 0.1f);
		_startTime = startTime;
		ringCount = Math.Clamp(ringCount, 1, 8);
		int segments = 48;
		for (int i = 0; i < ringCount; i++)
		{
			MagicCircle circle = new MagicCircle(_center, 0.01f, 0f, color, width, segments, 0, 2f + i * 1.8f);
			_rings.Add(new Ring { Circle = circle, Delay = i * 0.18f, Segments = segments });
			BeamCount += segments;
		}
	}

	public void Update(float now)
	{
		bool active = false;
		foreach (Ring ring in _rings)
		{
			float p = Math.Clamp((now - _startTime - ring.Delay) / _seconds, 0f, 1f);
			if (p < 1f)
			{
				active = true;
			}
			float ease = 1f - (1f - p) * (1f - p);
			ring.Circle.SetRadius(MathF.Max(_radius * ease, 0.01f), 0f);
			ring.Circle.Update(_center, 0f);
		}
		Finished = !active;
	}

	public void Remove()
	{
		foreach (Ring ring in _rings)
		{
			ring.Circle.Remove();
		}
		_rings.Clear();
		BeamCount = 0;
	}

	public int RemoveChunk(int budget)
	{
		int removed = 0;
		while (_rings.Count > 0 && removed < budget)
		{
			Ring ring = _rings[_rings.Count - 1];
			_rings.RemoveAt(_rings.Count - 1);
			removed += ring.Segments;
			BeamCount -= ring.Segments;
			ring.Circle.Remove();
		}
		return removed;
	}

	public bool IsEmpty => _rings.Count == 0;
}
