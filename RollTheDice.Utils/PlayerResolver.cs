#nullable enable
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// 把伤害钩子里的实体安全地解析成玩家控制器。
///
/// <para>反编译出来的 dice 常用 <c>((NativeObject)entity).As&lt;CCSPlayerPawn&gt;()</c> 再读 <c>.Controller</c>，
/// 但伤害钩子的目标是<b>任意实体</b>（道具 / 世界 / 炸弹也会走这条钩子）：把非玩家实体当 pawn 读
/// 会按 pawn 的 schema 偏移越界读内存（错实体甚至原生崩）。这里只比对实体句柄，不对未知实体做 schema 读。</para>
///
/// <para>句柄表按 <see cref="Server.TickCount"/> 缓存：伤害高峰时每次伤害只做 O(1) 查询，
/// 每 tick 最多重建一次。</para>
/// </summary>
public static class PlayerResolver
{
	private static int _cacheTick = -1;
	private static readonly Dictionary<nint, CCSPlayerController> Cache = new();

	public static CCSPlayerController? FromEntity(CBaseEntity? entity)
	{
		if (entity == null || !((CEntityInstance)entity).IsValid)
		{
			return null;
		}
		nint handle = ((CEntityInstance)entity).Handle;
		if (handle == IntPtr.Zero)
		{
			return null;
		}

		int tick = Server.TickCount;
		if (tick != _cacheTick)
		{
			Rebuild();
			_cacheTick = tick;
		}

		return Cache.TryGetValue(handle, out CCSPlayerController? player) && player != null && player.IsValid
			? player
			: null;
	}

	private static void Rebuild()
	{
		Cache.Clear();
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			if (player == null || !player.IsValid)
			{
				continue;
			}
			CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
			if (pawn != null && ((CEntityInstance)pawn).IsValid)
			{
				Cache[((CEntityInstance)pawn).Handle] = player;
			}
		}
	}
}
