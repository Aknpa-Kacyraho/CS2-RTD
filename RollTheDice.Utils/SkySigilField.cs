#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// "坠落天空"的**头顶天空法阵群**：在落点上方的整片高空，由低到高叠出数十个蓝白色水平法阵。
///
/// <para>每层是一个精简刻画（外环 + 刻度环 + 星形 + 偶尔符文带），大小随高度递减形成嵌套观感，
/// 层间正反异速旋转，像精密齿轮组啮合。</para>
///
/// <para>动画：每层有 <c>SpawnTime</c>，到点才创建，并在 <c>growSeconds</c> 内 ease-out 展开——
/// 所以是"一层层弹出来"而非一帧铺满。倒数末尾整体可向内合拢（<c>contract</c>）。</para>
/// </summary>
public sealed class SkySigilField : IBeamGroup
{
	private sealed class Layer
	{
		public int Index;
		public float Height;
		public float Radius;
		public float Spin;
		public float SpawnTime;
		public float LastScale = -1f;
		public bool Built;
		public List<SigilShape> Shapes = new List<SigilShape>();
	}

	private readonly List<Layer> _layers = new List<Layer>();
	private readonly SigilParams _params;
	private readonly SigilPalette _palette;
	private readonly float _width;
	private readonly float _growSeconds;

	public int BeamCount { get; private set; }

	public SkySigilField(Vector center, int count, float heightStart, float heightEnd,
		float radiusStart, float radiusEnd, float radiusAlternate, float startDelay, float layerDelay, float growSeconds,
		float spin, SigilParams parameters, SigilPalette palette, float width, float startTime)
	{
		_params = parameters;
		_palette = palette;
		_width = MathF.Max(width, 0.5f);
		_growSeconds = MathF.Max(growSeconds, 0.05f);

		count = Math.Clamp(count, 1, 48);
		float span = MathF.Max(count - 1, 1);
		float from = MathF.Max(radiusStart, 1f);
		float to = MathF.Max(radiusEnd, 1f);
		float alternate = Math.Clamp(radiusAlternate, 0.2f, 1f);
		for (int i = 0; i < count; i++)
		{
			float t = i / span;
			// 指数递减：线性插值在多层里等于是"微调"，相邻层看不出差异。
			float baseRadius = from * MathF.Pow(to / from, t);
			// 奇偶交替：大 / 小 / 大 / 小 —— 相邻层相差 ~2 倍，远看也明显"大小不一"。
			float radius = baseRadius * ((i % 2 == 0) ? 1f : alternate);
			_layers.Add(new Layer
			{
				Index = i,
				Height = heightStart + (heightEnd - heightStart) * t,
				Radius = radius,
				Spin = spin * ((i % 2 == 0) ? 1f : -1f) / (1f + i * 0.06f),
				SpawnTime = startTime + startDelay + i * layerDelay,
				Built = false
			});
		}
	}

	public void Update(Vector center, float now, float contract = 1f)
	{
		if (contract <= 0f)
		{
			contract = 0.01f;
		}
		foreach (Layer layer in _layers)
		{
			if (now < layer.SpawnTime)
			{
				continue;
			}
			if (!layer.Built)
			{
				Build(layer, center);
			}
			float grow = Math.Clamp((now - layer.SpawnTime) / _growSeconds, 0f, 1f);
			float ease = 1f - (1f - grow) * (1f - grow) * (1f - grow);
			Vector layerCenter = new Vector(center.X, center.Y, center.Z + layer.Height);
			float scale = MathF.Max(ease * contract, 0.01f);
			bool statics = MathF.Abs(scale - layer.LastScale) > 0.004f;
			foreach (SigilShape shape in layer.Shapes)
			{
				if (shape.Animated || statics)
				{
					shape.Update(layerCenter, now, scale);
				}
			}
			if (statics)
			{
				layer.LastScale = scale;
			}
		}
	}

	private void Build(Layer layer, Vector center)
	{
		layer.Built = true;
		float r = layer.Radius;
		int seg = Math.Clamp((int)(r / 14f), 12, 56);
		int ticks = Math.Clamp((int)(r / 26f), 8, 40);
		Vector c = new Vector(center.X, center.Y, center.Z + layer.Height);
		SigilParams p = _params;

		layer.Shapes.Add(SigilBuilder.Arc(c, r, seg, 0f, _palette.Mid, _palette.Halo, _width * 0.9f, false));
		layer.Shapes.Add(SigilBuilder.TickRing(c, r * 0.93f, ticks, r * 0.06f, 1.5f, layer.Spin * -0.5f, _palette.Mid, _palette.Halo, _width * 0.8f, false));
		layer.Shapes.Add(SigilBuilder.Star(c, r * 0.74f, p.StarPoints, p.StarSkip, 2.6f, layer.Spin, _palette.Core, _palette.Halo, _width, p.DoubleLine));
		if (layer.Index % 4 == 0)
		{
			layer.Shapes.Add(SigilBuilder.RuneBand(c, r * 0.50f, r * 0.60f, Math.Clamp((int)(r / 22f), 12, 32), 3.4f, layer.Spin * 0.7f, p.Seed + layer.Index * 13, _palette.Core, _palette.Halo, _width * 0.8f, false));
		}

		foreach (SigilShape shape in layer.Shapes)
		{
			BeamCount += shape.BeamCount;
		}
	}

	public void Remove()
	{
		foreach (Layer layer in _layers)
		{
			foreach (SigilShape shape in layer.Shapes)
			{
				shape.Remove();
			}
			layer.Shapes.Clear();
		}
		BeamCount = 0;
	}

	public int RemoveChunk(int budget)
	{
		int removed = 0;
		for (int i = _layers.Count - 1; i >= 0 && removed < budget; i--)
		{
			Layer layer = _layers[i];
			while (layer.Shapes.Count > 0 && removed < budget)
			{
				SigilShape shape = layer.Shapes[layer.Shapes.Count - 1];
				layer.Shapes.RemoveAt(layer.Shapes.Count - 1);
				removed += shape.BeamCount;
				BeamCount -= shape.BeamCount;
				shape.Remove();
			}
		}
		return removed;
	}

	public bool IsEmpty
	{
		get
		{
			foreach (Layer layer in _layers)
			{
				if (layer.Shapes.Count > 0)
				{
					return false;
				}
			}
			return true;
		}
	}
}
