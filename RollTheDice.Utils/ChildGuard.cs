#nullable enable
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// 带父实体的子实体看门狗。
///
/// <para>凡是挂在别的实体上的子实体（<c>FollowEntity</c> / <c>SetParent</c> 造出来的 prop / 粒子），
/// 一旦父实体被删除，子实体的父链就变成悬垂引用——引擎之后计算它的世界坐标时会解引用已消失的 owner
/// 而原生崩溃（社区已知的「CBeam 悬垂 parent」崩溃类，见 BeamCrashFix）。这类崩溃在 dice 里很常见：
/// 玩家 pawn 在死亡/换边时会被引擎删除，而挂在它身上的发光 prop 往往要等到下一次清理才被删。</para>
///
/// <para>这里每 tick 检查一次：父实体失效就先把子实体的父链断掉再删除，兜住全部 dice 的这条崩溃路径。</para>
/// </summary>
public static class ChildGuard
{
	private sealed class Entry
	{
		public required CEntityInstance Child;
		public required CEntityInstance Parent;
	}

	private static readonly List<Entry> Watched = new List<Entry>();

	/// <summary>登记一个"只在父实体存活期间才允许存在"的子实体。</summary>
	public static void Track(CEntityInstance? child, CEntityInstance? parent)
	{
		if (child == null || parent == null || !child.IsValid)
		{
			return;
		}
		Watched.Add(new Entry { Child = child, Parent = parent });
	}

	/// <summary>父实体失效时立刻拆掉子实体（不在 tick 上）。</summary>
	private static void Kill(CEntityInstance child)
	{
		try
		{
			child.AcceptInput("ClearParent");
		}
		catch
		{
		}
		try
		{
			child.Remove();
		}
		catch
		{
		}
	}

	/// <summary>主插件每 tick 调用一次。</summary>
	public static void OnTick()
	{
		if (Watched.Count == 0)
		{
			return;
		}
		for (int i = Watched.Count - 1; i >= 0; i--)
		{
			Entry entry = Watched[i];
			if (!entry.Child.IsValid)
			{
				Watched.RemoveAt(i);
				continue;
			}
			if (entry.Parent.IsValid)
			{
				continue;
			}
			Kill(entry.Child);
			Watched.RemoveAt(i);
		}
	}

	/// <summary>卸载 / 回合切换时清空登记（实体本身由各 dice 的清理负责）。</summary>
	public static void ClearAll()
	{
		Watched.Clear();
	}
}
