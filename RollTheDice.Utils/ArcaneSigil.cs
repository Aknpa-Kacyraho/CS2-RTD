#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 地面主法阵：把"骨架圆"换成十几项不同半径的刻画（外框双环 / 符文带 / 刻度环 / 大五芒星 / 五边形 /
/// 顶点辐条 / 中环 / 八边形 / 内环 / 六芒星 / 小五芒星 / 阵眼环 / 阵眼十字）。
///
/// <para>刻画表已移到 <see cref="SigilStyles"/>（<see cref="SigilStyle.Arcane"/>），本类只剩容器职责：
/// 静态/动画分离（圆环旋转对称 → 只在跟随点/缩放变化超过阈值时重摆）由 <see cref="SigilShapeGroup"/> 承担。
/// 组合表见 `docs/superpowers/specs/2026-09-20-arcane-sigil-detail-design.md` §5。</para>
/// </summary>
public sealed class ArcaneGroundSigil : IBeamGroup
{
	private readonly SigilShapeGroup _group = new SigilShapeGroup();

	public int BeamCount => _group.BeamCount;

	public ArcaneGroundSigil(Vector center, float radius, SigilParams p, SigilPalette pal, float width, float spin = 0.9f)
	{
		_group.AddRange(SigilStyles.Build(SigilStyle.Arcane, center, radius, p, pal, width, spin, 1f, true));
	}

	public void Update(Vector center, float time, float scale = 1f)
	{
		_group.Update(center, time, scale);
	}

	public void Remove()
	{
		_group.Remove();
	}

	public bool IsEmpty => _group.IsEmpty;

	/// <summary>
	/// 分帧拆除：最多拆掉约 <paramref name="budget"/> 根 CBeam，返回实际拆掉的根数。
	/// 目的：引爆/结束时不在一帧里删除上千实体，避免与爆炸实体创建挤在同一帧。
	/// </summary>
	public int RemoveChunk(int budget)
	{
		return _group.RemoveChunk(budget);
	}
}

/// <summary>
/// 竖向法阵塔：每层 = 1 个细底环（旋转对称 → 静态）+ 1 种刻画（动画），8 层循环不同刻画，
/// 不再是"8 层一个样"。层层抬升、正反交替旋转，整体可收缩。
/// </summary>
public sealed class ArcaneTower : IBeamGroup
{
	private sealed class Level
	{
		public SigilShape Base = null!;
		public SigilShape Feature = null!;
		public float Height;
	}

	private readonly List<Level> _levels = new List<Level>();
	private readonly Vector _origin;
	private float _lastScale = 1f;

	public int BeamCount { get; private set; }

	public ArcaneTower(Vector center, float radius, SigilParams p, SigilPalette pal, float width, int count, float heightBase, float heightStep, float radiusDecay, float spin)
	{
		_origin = new Vector(center.X, center.Y, center.Z);
		count = Math.Clamp(count, 1, 16);
		radiusDecay = Math.Clamp(radiusDecay, 0f, 0.6f);
		float r = radius;
		for (int i = 0; i < count; i++)
		{
			float h = heightBase + heightStep * i;
			Vector levelCenter = new Vector(center.X, center.Y, center.Z + h);
			Color color = (i % 2 == 0) ? pal.Core : pal.Mid;
			SigilShape baseRing = SigilBuilder.Arc(levelCenter, r, p.Scaled(48), 0f, color, pal.Halo, width * 0.8f, false);
			SigilShape feature = BuildFeature(i, levelCenter, r, p, pal, width, spin);
			Level level = new Level { Base = baseRing, Feature = feature, Height = h };
			_levels.Add(level);
			BeamCount += baseRing.BeamCount + feature.BeamCount;
			r *= 1f - radiusDecay;
		}
	}

	private static SigilShape BuildFeature(int index, Vector center, float r, SigilParams p, SigilPalette pal, float width, float spin)
	{
		bool dl = p.DoubleLine;
		switch (index % 8)
		{
		case 0:
		case 5:
			return SigilBuilder.RuneBand(center, r * 0.80f, r * 0.98f, p.Scaled(48), 1.0f, spin * 0.6f, p.Seed + index * 17, pal.Mid, pal.Halo, width, false);
		case 1:
		case 6:
			return SigilBuilder.Star(center, r * 0.92f, p.StarPoints, p.StarSkip, 1.0f, spin, pal.Core, pal.Halo, width, dl);
		case 2:
			return SigilBuilder.Polygon(center, r * 0.92f, p.PolygonSides, 1.0f, -spin * 0.7f, pal.Mid, pal.Halo, width, false);
		case 3:
			return SigilBuilder.TickRing(center, r * 0.95f, p.Scaled(24), r * 0.05f, 1.0f, -spin * 0.5f, pal.Mid, pal.Halo, width, false);
		case 4:
			return SigilBuilder.Star(center, r * 0.92f, 6, 2, 1.0f, spin, pal.Mid, pal.Halo, width * 0.8f, dl);
		default:
			return SigilBuilder.Spokes(center, r * 0.35f, r * 0.98f, SigilBuilder.StarAngles(6), 1.0f, spin, pal.Core, pal.Halo, width, dl);
		}
	}

	public void Update(Vector center, float time, float scale = 1f)
	{
		if (scale <= 0f)
		{
			scale = 0.01f;
		}
		bool statics = MathF.Abs(scale - _lastScale) > 0.004f || Vectors.GetDistance(_origin, center) > 1f;
		foreach (Level level in _levels)
		{
			Vector c = new Vector(center.X, center.Y, center.Z + level.Height);
			if (statics)
			{
				level.Base.Update(c, time, scale);
			}
			level.Feature.Update(c, time, scale);
		}
		if (statics)
		{
			_lastScale = scale;
		}
	}

	public void Remove()
	{
		foreach (Level level in _levels)
		{
			level.Base.Remove();
			level.Feature.Remove();
		}
		_levels.Clear();
		BeamCount = 0;
	}

	public bool IsEmpty => _levels.Count == 0;

	/// <summary>分帧拆除：逐层拆，最多拆掉约 <paramref name="budget"/> 根 CBeam。</summary>
	public int RemoveChunk(int budget)
	{
		int removed = 0;
		while (_levels.Count > 0 && removed < budget)
		{
			int last = _levels.Count - 1;
			Level level = _levels[last];
			_levels.RemoveAt(last);
			int count = level.Base.BeamCount + level.Feature.BeamCount;
			removed += count;
			BeamCount -= count;
			level.Base.Remove();
			level.Feature.Remove();
		}
		return removed;
	}
}

/// <summary>
/// 法阵的"分帧拆除"队列：引爆/结束时不在一帧里删掉上千 CBeam，
/// 而是每 tick 拆一小批，并把拆除与爆炸实体创建分到不同帧。
/// </summary>
public sealed class SigilTeardown
{
	private sealed class Entry
	{
		public List<IBeamGroup> Groups = new List<IBeamGroup>();
	}

	private readonly List<Entry> _entries = new List<Entry>();

	public bool Any => _entries.Count > 0;

	/// <summary>兼容旧调用（地面法阵 + 竖向法阵塔）。</summary>
	public void Add(MagicSigil? ground, MagicTower? tower, Vector center)
	{
		List<IBeamGroup> groups = new List<IBeamGroup>();
		if (ground != null)
		{
			groups.Add(ground);
		}
		if (tower != null)
		{
			groups.Add(tower);
		}
		Add(groups);
	}

	/// <summary>通用重载：任意 beam 组合（穹顶 / 天空法阵群 / 冲击环…）都走同一条分帧拆除队列。</summary>
	public void Add(IEnumerable<IBeamGroup> groups)
	{
		List<IBeamGroup> list = new List<IBeamGroup>();
		foreach (IBeamGroup group in groups)
		{
			if (group != null && !group.IsEmpty)
			{
				list.Add(group);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		_entries.Add(new Entry { Groups = list });
	}

	/// <summary>旧签名（center 未被使用，保留兼容）。</summary>
	public void Add(IEnumerable<IBeamGroup> groups, Vector center)
	{
		Add(groups);
	}

	/// <summary>
	/// 每 tick 拆一批。默认每批约 <b>120</b> 根：分帧拆除的目的就是"别让引擎在短时间里吞掉上千实体删除"，
	/// 300/tick 实测仍偏猛（客户端实体表容易错位 → CopyExistingEntity 致命错误），故调低到 120/tick
	/// （3700 根 ≈ 31 tick ≈ 0.5s 拆完，视觉上依然干净）。
	/// </summary>
	public void Tick(float now, int budgetPerEntry = 120)
	{
		for (int i = _entries.Count - 1; i >= 0; i--)
		{
			Entry entry = _entries[i];
			int budget = budgetPerEntry;
			List<IBeamGroup> alive = new List<IBeamGroup>();
			foreach (IBeamGroup group in entry.Groups)
			{
				if (!group.IsEmpty)
				{
					budget -= group.RemoveChunk(Math.Max(budget, 0));
				}
				if (!group.IsEmpty)
				{
					alive.Add(group);
				}
			}
			entry.Groups = alive;
			if (alive.Count == 0)
			{
				_entries.RemoveAt(i);
			}
		}
	}

	/// <summary>立即清空（回合结束 / 插件卸载）。</summary>
	public void Flush()
	{
		foreach (Entry entry in _entries)
		{
			foreach (IBeamGroup group in entry.Groups)
			{
				group.Remove();
			}
		}
		_entries.Clear();
	}
}
