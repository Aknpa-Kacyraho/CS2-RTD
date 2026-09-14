using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

/// <summary>
/// 作弊指令守卫：服务器 <c>sv_cheats 1</c> 时，拦掉**普通玩家**使用"动作类"作弊指令
/// （noclip/god/give/impulse/setpos/ent_* 等），防止加入的玩家开挂。
///
/// 设计原则（2026-09-14 修正）：
/// - 只拦"玩家动作类"作弊，**绝不碰** <c>sv_*</c> / <c>mp_*</c> / <c>bot_*</c> / <c>map</c> / <c>changelevel</c>
///   这些引擎/配置/游戏流程指令（之前误拦导致单机人机起不来）。
/// - 只有 **≥2 个真人** 时才启用（单机/本地人机只有房主 1 人 → 完全不拦截）。
/// - 拥有 <see cref="BypassPermission"/>（默认房主管理员 @css/root）永远放行。
/// - 客户端本地 cvar（透视 r_drawothermodels、线框 mat_wireframe 等）服务器收不到，拦不了。
/// </summary>
public class CheatGuardConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>拥有该权限的玩家（房主/管理员）不受限制，默认 CSS root。</summary>
	[JsonPropertyName("bypass_permission")]
	public string BypassPermission { get; set; } = "@css/root";

	/// <summary>
	/// 只放"玩家动作类"作弊指令。**不要**加 sv_*/mp_*/bot_*/map/changelevel（会拦到引擎和 cfg），
	/// 也不要加 host_timescale/weapon_*（dice 相关，虽然 dice 走服务器/ReplicateConVar 不会命中，但避免任何误伤）。
	/// </summary>
	[JsonPropertyName("blocked_commands")]
	public List<string> BlockedCommands { get; set; } = new List<string>
	{
		// 移动/生存类作弊
		"noclip", "god", "buddha", "notarget", "hurtme",
		// 物品/武器
		"give", "impulse", "drop",
		// 传送/位置
		"setpos", "setang", "setpos_exact", "setang_exact", "getpos",
		// 实体刷取/编辑
		"ent_create", "ent_remove", "ent_remove_all", "ent_fire", "ent_dump", "ent_info", "ent_text", "ent_teleport", "ent_bbox",
		// 视角
		"thirdperson",
	};
}
