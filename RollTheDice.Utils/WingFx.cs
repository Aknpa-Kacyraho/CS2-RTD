#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

/// <summary>
/// 用 <c>CBeam</c> 画一对<b>跟随玩家朝向</b>的光翼（每侧若干羽片，从肩背向外上方扇形张开）。
///
/// 为什么不用现成 vpcf：<c>particles/ui/status_levels/ui_status_level_wings.vpcf</c> 是 UI 状态等级
/// 复合粒子，子粒子在生成点 world-space 固化，服务器每 tick <c>Teleport</c> 发射器也带不走它，实测"不跟人"。
/// 素材里也没有可跟随的羽翼粒子，所以改用 CBeam 自造：起终点完全由服务器掌握，位置 / 朝向 / 大小 / 扇动都能精确控制，
/// 且无素材依赖。几何与 <see cref="MagicCircle"/> 同思路：只创建一次光束，之后每 tick 重算起终点。
/// </summary>
public sealed class WingFx
{
	private readonly struct Blade
	{
		public readonly CBeam Beam;
		public readonly int Side;   // -1 左, +1 右
		public readonly int Index;  // 0 = 内侧（更竖直）, Count-1 = 外侧（更外展）

		public Blade(CBeam beam, int side, int index)
		{
			Beam = beam;
			Side = side;
			Index = index;
		}
	}

	private readonly List<Blade> _blades = new List<Blade>();
	private readonly int _count;
	private readonly float _length;
	private readonly float _rootSpread;
	private readonly float _back;
	private readonly float _z;
	private readonly float _flapAmount;

	/// <param name="rootSpread">背部根点左右间距（通常传 <c>FxGeom.Spread</c>）。</param>
	/// <param name="back">根点在背部的后移距离（通常传 <c>FxGeom.Back</c>）。</param>
	/// <param name="z">根点高度（通常传 <c>FxGeom.WingZ</c>）。</param>
	/// <param name="flapAmount">扇动幅度（度）。</param>
	public WingFx(Color color, float width, int blades, float length, float rootSpread, float back, float z, float flapAmount = 10f)
	{
		_count = Math.Clamp(blades, 1, 8);
		_length = MathF.Max(length, 10f);
		_rootSpread = rootSpread;
		_back = back;
		_z = z;
		_flapAmount = flapAmount;
		for (int side = -1; side <= 1; side += 2)
		{
			for (int i = 0; i < _count; i++)
			{
				CBeam? beam = BeamFx.Create(color, width);
				if (beam != null)
				{
					_blades.Add(new Blade(beam, side, i));
				}
			}
		}
	}

	/// <summary>全部光束创建失败（无有效羽片）时为 true，调用方应丢弃该实例。</summary>
	public bool IsEmpty => _blades.Count == 0;

	/// <summary>每 tick 调用：按玩家原点与朝向重算两翼全部羽片的起终点。</summary>
	public void Update(Vector origin, float yaw, float flapPhase)
	{
		float fwdX = MathF.Cos(yaw);
		float fwdY = MathF.Sin(yaw);
		float rightX = MathF.Sin(yaw);
		float rightY = 0f - MathF.Cos(yaw);
		float flap = MathF.Sin(flapPhase) * _flapAmount;
		foreach (Blade blade in _blades)
		{
			float t = _count <= 1 ? 1f : (float)blade.Index / (_count - 1);
			// 展开角：内侧更竖直，外侧更外展，叠加扇动。
			float spreadDeg = 18f + t * 62f + flap;
			float rad = spreadDeg * MathF.PI / 180f;
			float sin = MathF.Sin(rad);
			float cos = MathF.Cos(rad);
			float len = _length * (0.55f + 0.45f * t);

			// 根点：背部，左右各偏移一点。
			float rootX = origin.X - fwdX * _back + rightX * (_rootSpread * 0.3f) * blade.Side;
			float rootY = origin.Y - fwdY * _back + rightY * (_rootSpread * 0.3f) * blade.Side;
			float rootZ = origin.Z + _z;
			Vector root = new Vector(rootX, rootY, rootZ);

			// 羽片方向 = 上 * cos + 外 * sin + 后 * 0.18。
			float dirX = rightX * blade.Side * sin - fwdX * 0.18f;
			float dirY = rightY * blade.Side * sin - fwdY * 0.18f;
			float dirZ = cos;
			float mag = MathF.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
			if (mag < 0.0001f)
			{
				mag = 1f;
			}
			Vector tip = new Vector(rootX + dirX / mag * len, rootY + dirY / mag * len, rootZ + dirZ / mag * len);

			BeamFx.Move(blade.Beam, root, tip);
		}
	}

	public void Remove()
	{
		foreach (Blade blade in _blades)
		{
			BeamFx.Kill(blade.Beam);
		}
		_blades.Clear();
	}
}
