#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// CBeam 特效的公共原语：创建 / 移动 / 销毁 / 光柱 / 魔法阵。
///
/// 为什么是 CBeam：CS2 服务器无法通用放大或转向现成 vpcf（见
/// <c>docs/research/2026-09-20-cs2-particle-system.md</c>），而 CBeam 的起终点完全由服务器掌握，
/// 大小 / 朝向 / 位置 / 跟随都能精确控制，且无需自制素材。
/// </summary>
public static class BeamFx
{
	public static CBeam? Create(Color color, float width)
	{
		CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");
		if (beam == null || !beam.IsValid)
		{
			return null;
		}
		((CBaseModelEntity)beam).Render = color;
		beam.Width = width;
		// 等宽：默认 EndWidth 若为 0 会向末端收成尖，看起来又细又小。
		beam.EndWidth = width;
		// 只 spawn 一次；之后每 tick 只 Teleport/改 EndPos，绝不重复 spawn。
		((CBaseEntity)beam).DispatchSpawn();
		return beam;
	}

	public static void Move(CBeam? beam, Vector start, Vector end)
	{
		if (beam == null || !beam.IsValid)
		{
			return;
		}
		try
		{
			((CBaseEntity)beam).Teleport(start, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
			beam.EndPos.X = end.X;
			beam.EndPos.Y = end.Y;
			beam.EndPos.Z = end.Z;
			// ★ 关键：直接改 m_vecEndPos 不会自动同步给客户端，必须显式标脏，否则 beam 永远停在初始位置
			// （症状：跟随类光束不跟随、旋转/收缩的魔法阵纹丝不动）。
			Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
		}
		catch
		{
		}
	}

	public static bool IsAlive(CBeam? beam)
	{
		return beam != null && beam.IsValid;
	}

	public static void Kill(CBeam? beam)
	{
		if (beam == null)
		{
			return;
		}
		try
		{
			if (beam.IsValid)
			{
				((CEntityInstance)beam).Remove();
			}
		}
		catch
		{
		}
	}

	public static void KillAll(IEnumerable<CBeam?> beams)
	{
		if (beams == null)
		{
			return;
		}
		foreach (CBeam? beam in beams)
		{
			Kill(beam);
		}
	}

	/// <summary>从地面点向上竖起一根光柱（外粗内白）。</summary>
	public static void Pillar(Vector? ground, float height, Color color, float width, float? lifeSeconds = null)
	{
		Effects.BeamColumn(ground, height, color, width, lifeSeconds);
	}

	/// <summary>水平铺地的魔法阵句柄（圆环 + 辐条 + 外环），可旋转 / 改半径 / 跟随。</summary>
	public static MagicCircle Ring(Vector center, float radius, float outerRadius, Color color, float width, int segments, int spokes, float zOffset = 4f)
	{
		return new MagicCircle(center, radius, outerRadius, color, width, segments, spokes, zOffset);
	}
}
