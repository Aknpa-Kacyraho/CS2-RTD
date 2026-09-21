#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 多层同心 CBeam 法阵：把 <see cref="MagicCircle"/> 叠成"超位魔法"式的巨型魔法阵。
///
/// 特征：外层最大（带外环），内层逐层缩小、正反交替旋转；支持整体收缩（倒数合拢）和每 tick 跟随。
/// 圆环 / 辐条全部由服务器画 CBeam，无素材依赖，大小和范围完全可控（见
/// <c>docs/research/2026-09-20-cs2-particle-system.md</c>：现成 vpcf 无法通用放大 / 转向）。
/// </summary>
public sealed class MagicSigil : IBeamGroup
{
	private sealed class Layer
	{
		public MagicCircle Circle = null!;
		public float BaseRadius;
		public float BaseOuter;
		public float Spin;
	}

	private readonly List<Layer> _layers = new List<Layer>();
	private ArcaneGroundSigil? _arcane;

	/// <summary>新版"多层次刻画"法阵（五芒星 / 符文带 / 刻度环 / 内嵌多边形…）。</summary>
	public MagicSigil(Vector center, float radius, SigilParams parameters, SigilPalette palette, float width, float spin = 0.9f)
	{
		_arcane = new ArcaneGroundSigil(center, radius, parameters, palette, width, spin);
	}

	public MagicSigil(Vector center, float radius, float outerRadius, Color color, float width, int segments, int spokes, int layerCount, float layerShrink, float spin, float zOffset = 4f)
	{
		layerCount = Math.Clamp(layerCount, 1, 8);
		layerShrink = Math.Clamp(layerShrink, 0f, 0.8f);
		float r = radius;
		float o = outerRadius;
		for (int i = 0; i < layerCount; i++)
		{
			if (i > 0 && r < 40f)
			{
				break;
			}
			// 偶数层正转、奇数层反转；层越大转得越慢，避免整体像一块死板。
			float layerSpin = spin * (i % 2 == 0 ? 1f : -1f) / (1f + i * 0.45f);
			float layerOuter = i == 0 ? o : 0f;
			int layerSpokes = Math.Max(spokes - i * 2, 0);
			MagicCircle circle = new MagicCircle(center, r, layerOuter, color, width, segments, layerSpokes, zOffset + i * 2f);
			_layers.Add(new Layer { Circle = circle, BaseRadius = r, BaseOuter = layerOuter, Spin = layerSpin });
			r *= 1f - layerShrink;
			o *= 1f - layerShrink;
		}
	}

	/// <summary>重新定位并旋转。<paramref name="scale"/> 用于整体收缩（1 = 原始大小，&lt;1 向内合拢）。</summary>
	public void Update(Vector center, float time, float scale = 1f)
	{
		if (scale <= 0f)
		{
			scale = 0.01f;
		}
		if (_arcane != null)
		{
			_arcane.Update(center, time, scale);
			return;
		}
		foreach (Layer layer in _layers)
		{
			layer.Circle.SetRadius(layer.BaseRadius * scale, layer.BaseOuter * scale);
			layer.Circle.Update(center, time * layer.Spin);
		}
	}

	public void Remove()
	{
		if (_arcane != null)
		{
			_arcane.Remove();
			return;
		}
		foreach (Layer layer in _layers)
		{
			layer.Circle.Remove();
		}
		_layers.Clear();
	}

	public bool IsEmpty => _arcane != null ? _arcane.IsEmpty : _layers.Count == 0;

	/// <summary>分帧拆除（arcane 支持；legacy 直接全拆）。</summary>
	public int RemoveChunk(int budget)
	{
		if (_arcane != null)
		{
			return _arcane.RemoveChunk(budget);
		}
		Remove();
		return 1 << 20;
	}
}
