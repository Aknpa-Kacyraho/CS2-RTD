using System;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

public static class Vectors
{
	public static float GetDistance(Vector a, Vector b)
	{
		float num = a.X - b.X;
		float num2 = a.Y - b.Y;
		float num3 = a.Z - b.Z;
		return MathF.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public static QAngle GetLookAtAngle(Vector source, Vector target)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		Vector val = new Vector((float?)(target.X - source.X), (float?)(target.Y - source.Y), (float?)(target.Z - source.Z));
		float value = (float)(Math.Atan2(val.Y, val.X) * 180.0 / Math.PI);
		float num = MathF.Sqrt(val.X * val.X + val.Y * val.Y);
		float value2 = (float)((0.0 - Math.Atan2(val.Z, num)) * 180.0 / Math.PI);
		return new QAngle((float?)value2, (float?)value, (float?)0f);
	}

	public static Vector Normalize(Vector v)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		float num = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
		return (num > 0f) ? new Vector((float?)(v.X / num), (float?)(v.Y / num), (float?)(v.Z / num)) : new Vector((float?)0f, (float?)0f, (float?)0f);
	}

	public static Vector Cross(Vector a, Vector b)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		return new Vector((float?)(a.Y * b.Z - a.Z * b.Y), (float?)(a.Z * b.X - a.X * b.Z), (float?)(a.X * b.Y - a.Y * b.X));
	}

	/// <summary>
	/// 点到线段的最短距离；点在线段正投影范围外时返回 <see cref="float.PositiveInfinity"/>（视为未命中）。
	/// 用于光炮 / 激光类"沿一条线段命中"的判定（Excalibur；Supernova 另有其私有实现）。
	/// </summary>
	public static float DistanceToSegment(Vector point, Vector a, Vector b)
	{
		float abx = b.X - a.X;
		float aby = b.Y - a.Y;
		float abz = b.Z - a.Z;
		float apx = point.X - a.X;
		float apy = point.Y - a.Y;
		float apz = point.Z - a.Z;
		float ab2 = abx * abx + aby * aby + abz * abz;
		if (ab2 <= 0.0001f)
		{
			return MathF.Sqrt(apx * apx + apy * apy + apz * apz);
		}
		float t = (apx * abx + apy * aby + apz * abz) / ab2;
		if (t < 0f || t > 1f)
		{
			return float.PositiveInfinity;
		}
		float dx = apx - abx * t;
		float dy = apy - aby * t;
		float dz = apz - abz * t;
		return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
	}
}
