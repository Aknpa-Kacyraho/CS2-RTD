#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 苍白色的**半球立体穹顶**——"坠落天空"施法前摇的构筑体。
///
/// <para>几何：以落点为球心的半球面（纬线 + 经线两组 CBeam 网格），从赤道向上生长到顶点，
/// 内部再叠 2 圈符文带做"万花筒流转"。半径默认 ~480u（≈10m），随施法者/落点固定。</para>
///
/// <para>动画：每个面片带自己的 <c>SpawnTime</c>，按纬度自下而上分批创建；创建后在
/// <c>growSeconds</c> 内以 ease-out 从 0.05 放大到 1，所以是"从地面长出来"而不是一帧冒出来。
/// 临近发动时 <see cref="SetPulse"/> 用加宽 + 提亮做亮度脉冲（提亮失败也不影响加宽观感）。</para>
/// </summary>
public sealed class SigilDome : IBeamGroup
{
	private sealed class Facet
	{
		public float Theta1;
		public float Phi1;
		public float Theta2;
		public float Phi2;
		public float SpawnTime;
		public float Width;
		public Color Color;
		public bool Animated;
		public float LastGrow = -1f;
		public CBeam? Beam;
	}

	private readonly List<Facet> _facets = new List<Facet>();
	private readonly List<SigilShape> _bands = new List<SigilShape>();
	private readonly Vector _origin;
	private readonly float _radius;
	private readonly SigilPalette _palette;
	private readonly float _width;
	private readonly float _spin;
	private readonly float _growSeconds;
	private readonly float _facetGrow;
	private readonly float _startTime;
	private bool _bandsBuilt;
	private float _appliedPulse = -1f;

	public int BeamCount { get; private set; }

	public SigilDome(Vector center, float radius, SigilPalette palette, float width,
		int latitudeRings, int segments, int meridians, int meridianSegments,
		float spin, float growSeconds, float startTime)
	{
		_origin = new Vector(center.X, center.Y, center.Z);
		_radius = MathF.Max(radius, 16f);
		_palette = palette;
		_width = MathF.Max(width, 0.5f);
		_spin = spin;
		_growSeconds = MathF.Max(growSeconds, 0.05f);
		_startTime = startTime;

		latitudeRings = Math.Clamp(latitudeRings, 2, 24);
		segments = Math.Clamp(segments, 8, 128);
		meridians = Math.Clamp(meridians, 2, 48);
		meridianSegments = Math.Clamp(meridianSegments, 2, 24);

		// 每个面片自己的展开时长 = 纬度批间隔 × 2，避免整段生长拖成 2×growSeconds。
		_facetGrow = MathF.Max(_growSeconds / latitudeRings * 2f, 0.15f);
		// 同一圈内再按角度给一点点相位差，让纬线"扫过去"而不是整圈齐刷刷冒出来。
		float sweep = MathF.Min(_facetGrow * 0.5f, 0.25f);

		float[] ringSpawn = new float[latitudeRings + 1];
		for (int i = 0; i <= latitudeRings; i++)
		{
			ringSpawn[i] = startTime + _growSeconds * i / latitudeRings;
		}

		// 纬线：theta 从赤道(0)到顶点(PI/2)，一圈一圈往上。
		for (int i = 0; i <= latitudeRings; i++)
		{
			float theta = i * (MathF.PI * 0.5f) / latitudeRings;
			bool edge = (i == 0 || i == latitudeRings);
			Color color = (i % 2 == 0) ? palette.Core : palette.Mid;
			float w = edge ? _width * 1.2f : _width * 0.8f;
			for (int j = 0; j < segments; j++)
			{
				_facets.Add(new Facet
				{
					Theta1 = theta,
					Phi1 = j * 2f * MathF.PI / segments,
					Theta2 = theta,
					Phi2 = (j + 1) * 2f * MathF.PI / segments,
					SpawnTime = ringSpawn[i] + (j / (float)segments) * sweep,
					Width = w,
					Color = color,
					Animated = false
				});
			}
		}

		// 经线：赤道 → 顶点分段，每段归属其纬度圈的 SpawnTime。
		for (int m = 0; m < meridians; m++)
		{
			float phi = m * 2f * MathF.PI / meridians;
			for (int k = 0; k < meridianSegments; k++)
			{
				float t1 = k * (MathF.PI * 0.5f) / meridianSegments;
				float t2 = (k + 1) * (MathF.PI * 0.5f) / meridianSegments;
				int band = (int)MathF.Round(t2 / (MathF.PI * 0.5f) * latitudeRings);
				_facets.Add(new Facet
				{
					Theta1 = t1,
					Phi1 = phi,
					Theta2 = t2,
					Phi2 = phi,
					SpawnTime = ringSpawn[Math.Clamp(band, 0, latitudeRings)] + (m / (float)meridians) * sweep,
					Width = _width * 0.7f,
					Color = palette.Mid,
					Animated = true
				});
			}
		}
	}

	public void Update(Vector center, float now, float scale = 1f)
	{
		float rot = (now - _startTime) * _spin;
		float r = _radius * MathF.Max(scale, 0.01f);
		foreach (Facet facet in _facets)
		{
			float grow = (now - facet.SpawnTime) / _facetGrow;
			if (grow <= 0f)
			{
				continue;
			}
			// 纬线是旋转对称的圆环，转起来看不出变化 → 只在生长阶段重摆，之后省掉每 tick 的 Move。
			if (!facet.Animated && facet.Beam != null && MathF.Abs(grow - facet.LastGrow) < 0.01f)
			{
				continue;
			}
			facet.LastGrow = grow;
			float ease = EaseOut(Math.Clamp(grow, 0f, 1f));
			Vector p1 = Point(center, facet.Theta1, facet.Phi1 + rot, r * ease);
			Vector p2 = Point(center, facet.Theta2, facet.Phi2 + rot, r * ease);
			if (facet.Beam == null)
			{
				facet.Beam = BeamFx.Create(facet.Color, facet.Width);
				if (facet.Beam == null)
				{
					continue;
				}
				BeamCount++;
			}
			BeamFx.Move(facet.Beam, p1, p2);
		}

		// 内部符文带：比穹顶晚一点出现，自下而上点亮。
		float bandGrow = Math.Clamp((now - _startTime - _growSeconds * 0.35f) / MathF.Max(_growSeconds * 0.6f, 0.1f), 0f, 1f);
		if (!_bandsBuilt && bandGrow > 0f)
		{
			_bandsBuilt = true;
			BuildBands();
		}
		if (_bandsBuilt && bandGrow > 0f)
		{
			float bandScale = MathF.Max(scale * EaseOut(bandGrow), 0.01f);
			foreach (SigilShape band in _bands)
			{
				band.Update(center, now, bandScale);
			}
		}
	}

	private void BuildBands()
	{
		float r = _radius;
		_bands.Add(SigilBuilder.RuneBand(_origin, r * 0.70f, r * 0.78f, 36, r * 0.42f, _spin * 0.7f, 1201, _palette.Core, _palette.Halo, _width * 0.9f, false));
		_bands.Add(SigilBuilder.RuneBand(_origin, r * 0.46f, r * 0.52f, 26, r * 0.72f, _spin * -1.05f, 1307, _palette.Mid, _palette.Halo, _width * 0.8f, false));
	}

	/// <summary>亮度脉冲（0~1）：加宽并提亮整个穹顶；提亮无效时加宽仍能看出"发动信号"。</summary>
	public void SetPulse(float pulse)
	{
		pulse = Math.Clamp(pulse, 0f, 1f);
		if (MathF.Abs(pulse - _appliedPulse) < 0.08f)
		{
			return;
		}
		_appliedPulse = pulse;
		Color bright = Lerp(_palette.Core, Color.FromArgb(255, 255, 255, 255), pulse);
		foreach (Facet facet in _facets)
		{
			if (facet.Beam == null)
			{
				continue;
			}
			BeamFx.SetWidth(facet.Beam, facet.Width * (1f + pulse * 1.6f));
			BeamFx.SetColor(facet.Beam, pulse > 0.05f ? bright : facet.Color);
		}
	}

	public void Remove()
	{
		foreach (Facet facet in _facets)
		{
			BeamFx.Kill(facet.Beam);
			facet.Beam = null;
		}
		foreach (SigilShape band in _bands)
		{
			band.Remove();
		}
		_bands.Clear();
		BeamCount = 0;
	}

	public int RemoveChunk(int budget)
	{
		int removed = 0;
		for (int i = _facets.Count - 1; i >= 0 && removed < budget; i--)
		{
			Facet facet = _facets[i];
			if (facet.Beam == null)
			{
				continue;
			}
			BeamFx.Kill(facet.Beam);
			facet.Beam = null;
			BeamCount--;
			removed++;
		}
		while (_bands.Count > 0 && removed < budget)
		{
			SigilShape band = _bands[_bands.Count - 1];
			_bands.RemoveAt(_bands.Count - 1);
			removed += band.BeamCount;
			BeamCount -= band.BeamCount;
			band.Remove();
		}
		return removed;
	}

	public bool IsEmpty
	{
		get
		{
			foreach (Facet facet in _facets)
			{
				if (facet.Beam != null)
				{
					return false;
				}
			}
			return _bands.Count == 0;
		}
	}

	private static float EaseOut(float t)
	{
		return 1f - (1f - t) * (1f - t) * (1f - t);
	}

	private static Color Lerp(Color a, Color b, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return Color.FromArgb(
			(int)(a.A + (b.A - a.A) * t),
			(int)(a.R + (b.R - a.R) * t),
			(int)(a.G + (b.G - a.G) * t),
			(int)(a.B + (b.B - a.B) * t));
	}

	private static Vector Point(Vector c, float theta, float phi, float radius)
	{
		float flat = MathF.Cos(theta) * radius;
		return new Vector(c.X + MathF.Cos(phi) * flat, c.Y + MathF.Sin(phi) * flat, c.Z + MathF.Sin(theta) * radius);
	}
}
