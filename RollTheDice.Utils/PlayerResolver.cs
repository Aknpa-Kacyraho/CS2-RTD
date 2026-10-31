#nullable enable
using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// 把伤害钩子里的实体安全地解析成玩家控制器。
///
/// <para>反编译出来的 dice 常用 <c>((NativeObject)entity).As&lt;CCSPlayerPawn&gt;()</c> 再读 <c>.Controller</c>，
/// 但伤害钩子的目标是<b>任意实体</b>（道具 / 世界 / 炸弹也会走这条钩子）：把非玩家实体当 pawn 读
/// 会按 pawn 的 schema 偏移越界读内存（错实体甚至原生崩）。这里只比对实体句柄，不对未知实体做 schema 读。</para>
/// </summary>
public static class PlayerResolver
{
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
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
			if (pawn != null && ((CEntityInstance)pawn).Handle == handle)
			{
				return player;
			}
		}
		return null;
	}
}
