#nullable enable
using System;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 落点爆炸的**一根**竖直光柱：宽度（= 直径）在 <c>expandSeconds</c> 内由 <c>radiusStart</c> 迅速扩到
/// <c>radiusEnd</c>（ease-out，先猛地炸开再稳住），之后**脉动**着保持到存活结束。
///
/// <para>为什么只用一根：多根同心叠出来的"径向渐变"远看就是一层层同心圆（廉价感来源）；
/// 一根 + 光晕/flags（<see cref="BeamFx.CreateStyled"/>，spawn 前设好）+ 轻微脉动反而更像"能量柱"。</para>
///
/// <para>★ 2026-09-22 结论：绕轴的"肋条/箍环光柱壳"实测**很抽象、不如单根巨柱**，已拆掉，别再往回加。</para>
///
/// <para>伤害：<see cref="AccumulateDamage"/> 按 dps×dt 累积小数、只在攒够整数点时返回要扣的血量，
/// 所以"每秒 200"不会被 64tick 的取整吃掉（200/64≈3.125 → 靠累积仍有 200/s）。</para>
/// </summary>
public sealed class ExpandingBeamColumn : IBeamGroup
{
	private readonly CBeam? _beam;
	private readonly Vector _origin;
	private readonly float _height;
	private readonly float _radiusStart;
	private readonly float _radiusEnd;
	private readonly float _expandSeconds;
	private readonly float _life;
	private readonly float _startTime;
	private readonly float _pulseAmplitude;
	private readonly float _visualScale;
	private float _damageCarry;
	private float _lastAppliedWidth = -1f;

	public Vector Origin => _origin;

	public float Height => _height;

	/// <summary>光柱当前半径（伤害判定圆与视觉同步）。</summary>
	public float CurrentRadius { get; private set; }

	public float EndRadius => _radiusEnd;

	public ExpandingBeamColumn(Vector ground, float height, float radiusStart, float radiusEnd,
		float lifeSeconds, float expandSeconds, Color core, float startTime,
		float haloScale = 0f, int beamFlags = 0, float scrollSpeed = 0f, float pulseAmplitude = 0.05f, float visualScale = 1f)
	{
		_origin = new Vector(ground.X, ground.Y, ground.Z);
		_height = MathF.Max(height, 32f);
		_radiusStart = MathF.Max(radiusStart, 4f);
		_radiusEnd = MathF.Max(radiusEnd, _radiusStart);
		_life = MathF.Max(lifeSeconds, 0.3f);
		_expandSeconds = Math.Clamp(expandSeconds, 0.05f, _life);
		_startTime = startTime;
		_pulseAmplitude = Math.Clamp(pulseAmplitude, 0f, 0.3f);
		_visualScale = MathF.Max(visualScale, 0.2f);
		CurrentRadius = _radiusStart;

		Vector bottom = new Vector(_origin.X, _origin.Y, _origin.Z);
		Vector top = new Vector(_origin.X, _origin.Y, _origin.Z + _height);
		_beam = BeamFx.CreateStyled(bottom, top, core, _radiusStart * 2f * _visualScale, haloScale, beamFlags, scrollSpeed, (beamFlags & 12) != 0 ? _radiusEnd : 0f);
		if (_beam != null)
		{
			_lastAppliedWidth = _radiusStart * 2f * _visualScale;
		}
	}

	/// <summary>每 tick 推进展开/脉动。返回 false = 存活结束（调用方应立即 <see cref="Remove"/>）。</summary>
	public bool Update(float now)
	{
		float age = now - _startTime;
		if (age >= _life)
		{
			return false;
		}
		float t = Math.Clamp(age / _expandSeconds, 0f, 1f);
		float ease = 1f - (1f - t) * (1f - t);
		float radius = _radiusStart + (_radiusEnd - _radiusStart) * ease;
		// 稳住之后的轻微脉动：让柱子看起来是"能量"而不是一根静态管子。
		if (_pulseAmplitude > 0f)
		{
			radius *= 1f + _pulseAmplitude * MathF.Sin(age * 9f);
		}
		CurrentRadius = radius;
		float width = radius * 2f * _visualScale;
		if (MathF.Abs(width - _lastAppliedWidth) > MathF.Max(width * 0.01f, 0.5f))
		{
			_lastAppliedWidth = width;
			BeamFx.SetWidth(_beam, width);
		}
		return true;
	}

	/// <summary>按 dps×dt 累积，返回"这一帧该扣的整点血量"（0 = 还不够扣 1 点）。</summary>
	public int AccumulateDamage(float dps, float dt)
	{
		if (dps <= 0f)
		{
			return 0;
		}
		_damageCarry += dps * Math.Clamp(dt, 0f, 0.5f);
		int whole = (int)_damageCarry;
		if (whole <= 0)
		{
			return 0;
		}
		_damageCarry -= whole;
		return whole;
	}

	/// <summary>玩家是否在柱内（水平距圆心 ≤ 当前半径，且高度落在柱体内）。</summary>
	public bool Contains(Vector position)
	{
		float dx = position.X - _origin.X;
		float dy = position.Y - _origin.Y;
		float radius = CurrentRadius;
		if (dx * dx + dy * dy > radius * radius)
		{
			return false;
		}
		return position.Z >= _origin.Z - 120f && position.Z <= _origin.Z + _height;
	}

	public void Remove()
	{
		BeamFx.Kill(_beam);
	}

	public int RemoveChunk(int budget)
	{
		Remove();
		return 1 << 20;
	}

	public bool IsEmpty => !BeamFx.IsAlive(_beam);
}
