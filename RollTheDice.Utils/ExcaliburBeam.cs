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
/// 「对城之光」——Excalibur 释放阶段的光带（不是光柱）。
///
/// <para>喷涌：光带在 <c>beam_surge_seconds</c> 内从剑尖沿准星方向铺满全长（是喷涌、不是生长）；
/// 5 层同轴叠加（外光浪 620 → 白芯 80），各层纹理滚动速度不同 = 光的奔流；前端跟着一道
/// 垂直于光带的「光之断层」光墙（切断一切）。</para>
///
/// <para>擦地光浪：沿轴线依次点亮成对的斜向贴地光带（原作的光浪刮过地面）。</para>
///
/// <para>奔流光环：<c>ring_count</c> 个垂直光环沿轴线持续推进、循环重生。</para>
///
/// <para>判伤：只有 <c>beam_width</c>（主带）为伤害直径；外光浪纯视觉。淡出期不判伤。</para>
/// </summary>
public sealed class ExcaliburBeam : IBeamGroup
{
	private static readonly Color OuterColor = Color.FromArgb(255, 255, 226, 150);
	private static readonly Color GlowColor = Color.FromArgb(255, 255, 214, 90);
	private static readonly Color MainColor = Color.FromArgb(255, 255, 205, 60);
	private static readonly Color BrightColor = Color.FromArgb(255, 255, 240, 170);
	private static readonly Color CoreColor = Color.FromArgb(255, 255, 255, 255);
	private static readonly Color WaveColor = Color.FromArgb(255, 255, 220, 120);
	private static readonly Color RingColor = Color.FromArgb(255, 255, 232, 150);
	private static readonly Color WallColor = Color.FromArgb(255, 255, 245, 200);

	private readonly ExcaliburConfig _cfg;
	private readonly Vector _start;
	private readonly Vector _dir;
	private readonly Vector _right;
	private readonly Vector _perp;
	private readonly float _begin;
	private readonly float _groundZ;
	private readonly float _length;
	private readonly int _ringCount;
	private readonly int _ringSegments;
	private readonly int _waveCount;

	private readonly CBeam? _outer;
	private readonly CBeam? _glow;
	private readonly CBeam? _main;
	private readonly CBeam? _bright;
	private readonly CBeam? _core;
	private readonly CBeam? _wall;
	private readonly CBeam?[] _rings;
	private readonly List<CBeam?> _waves = new List<CBeam?>();

	private Vector _end = null!;
	private float _currentWidth;
	private float _maxWidth;
	private int _wavesSpawned;
	private float _holdStart;
	private float _fadeStart;
	private bool _reached;
	private bool _fading;

	public ExcaliburBeam(ExcaliburConfig cfg, Vector start, Vector forward, float now)
	{
		_cfg = cfg;
		_start = new Vector(start.X, start.Y, start.Z);
		Vector f = Vectors.Normalize(forward);
		_dir = (f.X == 0f && f.Y == 0f && f.Z == 0f) ? new Vector(1f, 0f, 0f) : f;
		Vector r = Vectors.Normalize(Vectors.Cross(_dir, new Vector(0f, 0f, 1f)));
		if (r.X == 0f && r.Y == 0f && r.Z == 0f)
		{
			r = new Vector(1f, 0f, 0f);
		}
		_right = r;
		_perp = Vectors.Normalize(Vectors.Cross(_right, _dir));
		_begin = now;
		_length = MathF.Max(cfg.BeamLength, 300f);
		_groundZ = start.Z - 42f;
		_waveCount = Math.Clamp(cfg.GroundWaveCount, 0, 24);
		_ringCount = Math.Clamp(cfg.RingCount, 0, 24);
		_ringSegments = Math.Clamp(cfg.RingSegments, 8, 48);
		_currentWidth = MathF.Max(cfg.BeamWidthStart, 4f);
		_maxWidth = MathF.Max(cfg.BeamWidth, _currentWidth);
		_end = new Vector(_start.X, _start.Y, _start.Z);

		// 5 层光带：起点先为 0 长，喷涌时一起向前铺；各层纹理滚动速度不同 = 光的奔流
		float w0 = _currentWidth;
		_outer = BeamFx.CreateStyled(_start, _start, OuterColor, w0 * 1.9f, 1.6f, 0, 150f);
		_glow = BeamFx.CreateStyled(_start, _start, GlowColor, w0 * 1.35f, 1.2f, 0, 175f);
		_main = BeamFx.CreateStyled(_start, _start, MainColor, w0, 0.8f, 0, 205f);
		_bright = BeamFx.CreateStyled(_start, _start, BrightColor, w0 * 0.6f, 0f, 0, 235f);
		_core = BeamFx.CreateStyled(_start, _start, CoreColor, w0 * 0.25f);
		// 前端「光之断层」光墙（垂直光带）；长度由 Update 每帧摆位
		_wall = BeamFx.Create(WallColor, MathF.Max(cfg.BeamWallWidth, 4f));

		// 奔流光环：全体先建，位置交给 Update
		_rings = new CBeam?[_ringCount * _ringSegments];
		for (int i = 0; i < _rings.Length; i++)
		{
			_rings[i] = BeamFx.Create(RingColor, 3.2f);
		}
		// 立即摆一次位：否则光墙 / 光环会先在 (0,0,0) 闪现一帧
		Update(now);
	}

	/// <summary>主带当前宽度（= 伤害直径，所见即所伤）。</summary>
	public float CurrentWidth => _currentWidth;

	/// <summary>光带当前覆盖的线段（喷涌中也有效；淡出期返回 false）。</summary>
	public bool TryGetSegment(out Vector start, out Vector end)
	{
		start = _start;
		end = _end;
		return !_fading;
	}

	/// <summary>推进喷涌 / 保持 / 淡出；返回 false = 结束（调用方应立即收尾）。</summary>
	public bool Update(float now)
	{
		if (_fading)
		{
			float t = Math.Clamp((now - _fadeStart) / 0.3f, 0f, 1f);
			float w = MathF.Max(_maxWidth * (1f - t), 0.5f);
			_currentWidth = w;
			ApplyWidths(w);
			FadeAuxiliary(t);
			return t < 1f;
		}

		float age = now - _begin;
		float surgeT = Math.Clamp(age / MathF.Max(_cfg.BeamSurgeSeconds, 0.02f), 0f, 1f);
		float surgeEase = 1f - (1f - surgeT) * (1f - surgeT);
		_end = new Vector(
			_start.X + _dir.X * _length * surgeEase,
			_start.Y + _dir.Y * _length * surgeEase,
			_start.Z + _dir.Z * _length * surgeEase);

		// 宽度扩张 + 保持期脉动
		float expandT = Math.Clamp(age / MathF.Max(_cfg.BeamExpandSeconds, 0.02f), 0f, 1f);
		float expandEase = 1f - (1f - expandT) * (1f - expandT);
		float width = MathF.Max(_cfg.BeamWidthStart + (_maxWidth - _cfg.BeamWidthStart) * expandEase, 1f);
		if (_reached)
		{
			width *= 1f + 0.04f * MathF.Sin((now - _holdStart) * 9f);
		}
		_currentWidth = width;
		ApplyWidths(width);

		// 5 层同步末端 + 前端断层光墙跟随
		BeamFx.Move(_outer, _start, _end);
		BeamFx.Move(_glow, _start, _end);
		BeamFx.Move(_main, _start, _end);
		BeamFx.Move(_bright, _start, _end);
		BeamFx.Move(_core, _start, _end);
		MoveWall(_end);

		// 擦地光浪按喷涌进度依次点亮；奔流光环推进
		SpawnWaves(surgeEase);
		UpdateRings(age);

		if (!_reached && surgeT >= 1f)
		{
			_reached = true;
			_holdStart = now;
		}
		if (_reached && now - _holdStart >= MathF.Max(_cfg.BeamLifeSeconds, 0.2f))
		{
			_fading = true;
			_fadeStart = now;
		}
		return true;
	}

	private void ApplyWidths(float w)
	{
		BeamFx.SetWidth(_outer, w * 1.9f);
		BeamFx.SetWidth(_glow, w * 1.35f);
		BeamFx.SetWidth(_main, w);
		BeamFx.SetWidth(_bright, w * 0.6f);
		BeamFx.SetWidth(_core, w * 0.25f);
	}

	/// <summary>前端断层：垂直于光带、横跨 wall_height 的宽光墙。</summary>
	private void MoveWall(Vector end)
	{
		float half = MathF.Max(_cfg.BeamWallHeight, 40f) * 0.5f;
		BeamFx.Move(_wall,
			new Vector(end.X - _right.X * half, end.Y - _right.Y * half, end.Z - _right.Z * half),
			new Vector(end.X + _right.X * half, end.Y + _right.Y * half, end.Z + _right.Z * half));
	}

	/// <summary>擦地光浪：一对向两侧斜前、微微上翘的贴地光带（光浪刮过地面）。</summary>
	private void SpawnWaves(float surgeEase)
	{
		if (_waveCount <= 0)
		{
			return;
		}
		while (_wavesSpawned < _waveCount && surgeEase >= (_wavesSpawned + 0.35f) / _waveCount)
		{
			float at = (_wavesSpawned + 0.5f) / _waveCount;
			_wavesSpawned++;
			Vector center = new Vector(
				_start.X + _dir.X * _length * at,
				_start.Y + _dir.Y * _length * at,
				_groundZ + 8f);
			float len = MathF.Max(_cfg.GroundWaveLength, 40f);
			float width = MathF.Max(_cfg.GroundWaveWidth, 2f);
			for (int side = -1; side <= 1; side += 2)
			{
				Vector b = new Vector(
					center.X + _right.X * side * len + _dir.X * len * 0.42f,
					center.Y + _right.Y * side * len + _dir.Y * len * 0.42f,
					center.Z + 34f);
				CBeam? wave = BeamFx.Create(WaveColor, width);
				if (wave != null)
				{
					BeamFx.Move(wave, center, b);
					_waves.Add(wave);
				}
			}
		}
	}

	/// <summary>奔流光环：p 沿轴线循环前进，半径随前进略收。</summary>
	private void UpdateRings(float age)
	{
		if (_ringCount <= 0 || _rings.Length == 0)
		{
			return;
		}
		float radiusBase = MathF.Max(_cfg.RingRadius, 40f);
		float speed = MathF.Max(_cfg.RingSpeed, 0.1f);
		for (int i = 0; i < _ringCount; i++)
		{
			float p = (age * speed + (float)i / _ringCount) % 1f;
			float dist = p * _length;
			float radius = radiusBase * (1f - 0.35f * p);
			Vector center = new Vector(
				_start.X + _dir.X * dist,
				_start.Y + _dir.Y * dist,
				_start.Z + _dir.Z * dist);
			for (int s = 0; s < _ringSegments; s++)
			{
				CBeam? beam = _rings[i * _ringSegments + s];
				if (beam == null)
				{
					continue;
				}
				float a1 = s * 2f * MathF.PI / _ringSegments;
				float a2 = (s + 1) * 2f * MathF.PI / _ringSegments;
				BeamFx.Move(beam, RingPoint(center, a1, radius), RingPoint(center, a2, radius));
			}
		}
	}

	private Vector RingPoint(Vector center, float angle, float radius)
	{
		float c = MathF.Cos(angle) * radius;
		float s = MathF.Sin(angle) * radius;
		return new Vector(
			center.X + _right.X * c + _perp.X * s,
			center.Y + _right.Y * c + _perp.Y * s,
			center.Z + _right.Z * c + _perp.Z * s);
	}

	/// <summary>淡出：光环 / 光浪 / 光墙一起收细。</summary>
	private void FadeAuxiliary(float t)
	{
		float ringW = MathF.Max(3.2f * (1f - t), 0.3f);
		foreach (CBeam? b in _rings)
		{
			BeamFx.SetWidth(b, ringW);
		}
		float waveW = MathF.Max(MathF.Max(_cfg.GroundWaveWidth, 2f) * (1f - t), 0.3f);
		foreach (CBeam? b in _waves)
		{
			BeamFx.SetWidth(b, waveW);
		}
		BeamFx.SetWidth(_wall, MathF.Max(MathF.Max(_cfg.BeamWallWidth, 4f) * (1f - t), 0.3f));
	}

	public void Remove()
	{
		BeamFx.Kill(_outer);
		BeamFx.Kill(_glow);
		BeamFx.Kill(_main);
		BeamFx.Kill(_bright);
		BeamFx.Kill(_core);
		BeamFx.Kill(_wall);
		for (int i = 0; i < _rings.Length; i++)
		{
			BeamFx.Kill(_rings[i]);
			_rings[i] = null;
		}
		foreach (CBeam? b in _waves)
		{
			BeamFx.Kill(b);
		}
		_waves.Clear();
	}

	public bool IsEmpty => BeamCount == 0;

	public int RemoveChunk(int budget)
	{
		Remove();
		return 1 << 20;
	}

	/// <summary>还活着的 CBeam 根数（调试回显）。</summary>
	public int BeamCount
	{
		get
		{
			int n = 0;
			if (BeamFx.IsAlive(_outer))
			{
				n++;
			}
			if (BeamFx.IsAlive(_glow))
			{
				n++;
			}
			if (BeamFx.IsAlive(_main))
			{
				n++;
			}
			if (BeamFx.IsAlive(_bright))
			{
				n++;
			}
			if (BeamFx.IsAlive(_core))
			{
				n++;
			}
			if (BeamFx.IsAlive(_wall))
			{
				n++;
			}
			foreach (CBeam? b in _rings)
			{
				if (BeamFx.IsAlive(b))
				{
					n++;
				}
			}
			foreach (CBeam? b in _waves)
			{
				if (BeamFx.IsAlive(b))
				{
					n++;
				}
			}
			return n;
		}
	}
}
