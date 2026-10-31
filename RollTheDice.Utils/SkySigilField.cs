#nullable enable
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>天空法阵群的观感 / 性能参数（由 dice config 映射）。</summary>
public sealed class SkySigilOptions
{
	/// <summary>"mixed" = 每层轮换风格；也可写 "arcane" / "ward" / "astrolabe" / "sun"。</summary>
	public string Style { get; set; } = "mixed";

	/// <summary>整体细节系数（0.2~1.5）：越小每层束数越少，是"卡不卡"的第一旋钮。</summary>
	public float Detail { get; set; } = 0.75f;

	/// <summary>参与自转的层数（0 = 全静态）。只有这些层走每 tick 更新，其余层几乎零成本。</summary>
	public int AnimatedLayers { get; set; } = 3;

	/// <summary>整片天空法阵的 CBeam 预算上限，按半径权重分给每层，超出就把该层最内侧的刻画摘掉。</summary>
	public int BeamBudget { get; set; } = 1600;

	/// <summary>自转图元的更新间隔（tick）：2 = 隔一 tick（省一半每 tick 网络），1 = 每 tick。</summary>
	public int AnimationStride { get; set; } = 2;

	/// <summary>大层外缘的立式短齿（"盘有厚度"，玩家在下方抬头看到的是盘的下沿）。</summary>
	public bool Rim { get; set; } = true;

	/// <summary>大层之间的斜向连接骨架（跨过中间小层 → 整片法阵成为"漏斗塔"而不是孤立圆盘）。</summary>
	public bool Spines { get; set; } = true;

	/// <summary>中央主轴（贯穿最低层→最高层）+ 大层轴承环。</summary>
	public bool Axis { get; set; } = true;
}

/// <summary>
/// "坠落天空"的**头顶天空法阵群**：在落点上方的整片高空，由低到高叠出数十个蓝白色水平法阵。
///
/// <para>每层是一套完整刻画（<see cref="SigilStyles"/>），风格按层轮换（mixed 模式相邻层不同、
/// 大层能轮到四种风格），大小随高度递减形成嵌套观感，层间正反异速旋转，像精密齿轮组啮合。</para>
///
/// <para>动画：每层有 <c>SpawnTime</c>，到点才创建，并在 <c>growSeconds</c> 内 ease-out 展开——
/// 所以是"一层层弹出来"而非一帧铺满。倒数末尾整体可向内合拢（<c>contract</c>）。</para>
///
/// <para>性能：① 只有 <see cref="SkySigilOptions.AnimatedLayers"/> 层带自转，其余层 <c>spin=0</c> →
/// 图元全部静态，容器跳过每 tick 的 Move/SetStateChanged；② 每层细节按半径与 <c>detail</c> 降采样；
/// ③ <see cref="SkySigilOptions.BeamBudget"/> 超预算时自动继续降采样，层数调大也不会失控。</para>
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
		public bool Built;
		public readonly SigilShapeGroup Group = new SigilShapeGroup();
	}

	private readonly List<Layer> _layers = new List<Layer>();
	private readonly SigilParams _params;
	private readonly SigilPalette _palette;
	private readonly float _width;
	private readonly float _growSeconds;
	private readonly IReadOnlyList<SigilStyle> _styles;
	private readonly float _detail;
	private readonly int _animatedLayers;
	private readonly int _budget;
	private readonly int _stride;
	private readonly float _radiusWeightSum;
	private readonly bool _rim;
	private readonly bool _spines;
	private readonly bool _axis;

	/// <summary>单层"风格刻画"的硬上限：同时兜底"单帧创建大量 CBeam"的风险（装置件不计入）。</summary>
	private const int MaxStyleBeamsPerLayer = 420;

	/// <summary>中央主轴绕中心小圆的半径（绝对单位，不随层缩放）。</summary>
	private const float AxisRadius = 52f;

	public int BeamCount
	{
		get
		{
			int total = 0;
			foreach (Layer layer in _layers)
			{
				total += layer.Group.BeamCount;
			}
			return total;
		}
	}

	public SkySigilField(Vector center, int count, float heightStart, float heightEnd,
		float radiusStart, float radiusEnd, float radiusAlternate, float startDelay, float layerDelay, float growSeconds,
		float spin, SigilParams parameters, SigilPalette palette, float width, float startTime, SkySigilOptions options)
	{
		_params = parameters;
		_palette = palette;
		_width = MathF.Max(width, 0.5f);
		_growSeconds = MathF.Max(growSeconds, 0.05f);
		_styles = SigilStyles.Pool(options.Style);
		_detail = Math.Clamp(options.Detail, 0.2f, 1.5f);
		_animatedLayers = Math.Clamp(options.AnimatedLayers, 0, 48);
		_budget = Math.Clamp(options.BeamBudget, 120, 8000);
		_stride = Math.Clamp(options.AnimationStride, 1, 4);
		_rim = options.Rim;
		_spines = options.Spines;
		_axis = options.Axis;

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
		foreach (Layer layer in _layers)
		{
			_radiusWeightSum += layer.Radius;
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
			layer.Group.Update(layerCenter, now, MathF.Max(ease * contract, 0.01f));
		}
	}

	private void Build(Layer layer, Vector center)
	{
		layer.Built = true;
		float r = layer.Radius;
		Vector c = new Vector(center.X, center.Y, center.Z + layer.Height);
		// 层越高/越小 → 细节越少（离得远看不清），再乘全局 detail。
		float detail = Math.Clamp(r / 1400f, 0.32f, 0.85f) * _detail;
		bool halo = detail >= 0.45f;
		// 只让最底下（最大、离玩家最近）的几层自转：动效看得见，每 tick 成本又只花在这几层上。
		bool animate = _animatedLayers > 0 && layer.Index % 2 == 0 && layer.Index / 2 < _animatedLayers;
		float spin = animate ? layer.Spin : 0f;
		layer.Group.AddRange(SigilStyles.Build(_styles[StyleIndex(layer.Index)], c, r, _params, _palette, _width, spin, detail, halo));
		layer.Group.Stride = _stride;
		// 硬预算：按半径权重分给每层；超了就从末尾（最内侧、最不显眼的刻画）往回摘，保证总束数有上限。
		// 单层上限同时兜底"单帧创建大量 CBeam"的风险。
		int cap = Math.Min(MaxStyleBeamsPerLayer, Math.Max(16, (int)(_budget * (r / MathF.Max(_radiusWeightSum, 1f)))));
		while (layer.Group.BeamCount > cap && !layer.Group.IsEmpty)
		{
			layer.Group.RemoveChunk(1);
		}
		// 装置件在预算之外：它们是"骨架"，被摘掉会很难看，所以砍完风格刻画之后才追加。
		layer.Group.AddRange(BuildDecor(layer, c));
	}

	/// <summary>
	/// 装置化件（全部静态、零每 tick 成本）：① 大层外缘**朝下的立式短齿**（盘有厚度）；
	/// ② 小层上挂**跨接两侧大层**的斜向骨架（整片法阵成为螺旋漏斗塔）；③ 最早层上的**中央主轴**；④ 大层中心轴承环。
	/// 坐标一律以**所在层的中心**为原点（<see cref="SigilShapeGroup.Update"/> 传的就是层中心）。
	/// </summary>
	private List<SigilShape> BuildDecor(Layer layer, Vector c)
	{
		List<SigilShape> shapes = new List<SigilShape>();
		float r = layer.Radius;
		bool big = layer.Index % 2 == 0;
		if (_rim && big)
		{
			shapes.Add(SigilBuilder.RimTicks(c, r, Math.Clamp((int)(r / 26f), 16, 44), 0f, 0f - r * 0.030f,
				7700 + layer.Index, _palette.Mid, _palette.Halo, _width * 0.8f, false));
		}
		if (_spines && !big && layer.Index > 0 && layer.Index + 1 < _layers.Count)
		{
			Layer prev = _layers[layer.Index - 1];
			Layer next = _layers[layer.Index + 1];
			float dPrev = prev.Height - layer.Height;
			float dNext = next.Height - layer.Height;
			SigilShape spine = new SigilShape(false, 0f, _palette.Mid, _palette.Halo, _width * 0.8f, false);
			float[] angles = SigilBuilder.StarAngles(6, layer.Index * 0.35f);
			for (int k = 0; k < angles.Length; k++)
			{
				spine.Add(prev.Radius, angles[k], dPrev, next.Radius, angles[k] + 0.22f, dNext);
			}
			spine.Update(c, 0f, 1f);
			shapes.Add(spine);
		}
		if (_axis && layer.Index == 0)
		{
			float hTop = 0f;
			foreach (Layer other in _layers)
			{
				hTop = MathF.Max(hTop, other.Height - layer.Height);
			}
			shapes.Add(SigilBuilder.Spokes3D(c, AxisRadius, AxisRadius, 0f, hTop, SigilBuilder.StarAngles(4), 0f,
				_palette.Core, _palette.Halo, _width, true));
		}
		if (_axis && big)
		{
			// 轴承环半径不能小于主轴本身，否则小一点的大层会"环在轴里"。
			shapes.Add(SigilBuilder.Arc(c, MathF.Max(r * 0.045f, AxisRadius * 1.25f), 10, 0.4f, _palette.Core, _palette.Halo, _width * 0.6f, false));
		}
		return shapes;
	}

	/// <summary>风格轮换：跳步取值让相邻层不同，且大层（偶数下标）能轮到池里每一种风格。</summary>
	private int StyleIndex(int index)
	{
		int len = _styles.Count;
		return len <= 1 ? 0 : (index + index / 2) % len;
	}

	public void Remove()
	{
		foreach (Layer layer in _layers)
		{
			layer.Group.Remove();
		}
	}

	public int RemoveChunk(int budget)
	{
		int removed = 0;
		for (int i = _layers.Count - 1; i >= 0 && removed < budget; i--)
		{
			removed += _layers[i].Group.RemoveChunk(Math.Max(budget - removed, 1));
		}
		return removed;
	}

	public bool IsEmpty
	{
		get
		{
			foreach (Layer layer in _layers)
			{
				if (!layer.Group.IsEmpty)
				{
					return false;
				}
			}
			return true;
		}
	}
}
