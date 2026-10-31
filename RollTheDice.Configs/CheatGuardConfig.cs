using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

/// <summary>
/// 作弊指令守卫：服务器 <c>sv_cheats 1</c> 时，拦掉**普通玩家**使用作弊指令 / 作弊 cvar
/// （noclip/god/give/sv_infinite_ammo/host_timescale 等），防止加入的玩家开挂。
///
/// 设计原则：
/// - **只拦玩家**：服务器控制台 / 插件（<c>Server.ExecuteCommand</c>）执行的命令 <c>player == null</c>，
///   一律放行 → cfg 里的 <c>sv_infinite_ammo</c>、dice 自己的 <c>host_timescale</c>、
///   <c>ReplicateConVar</c>（无扩散/雷达）都不受影响（这条是 2026-09-21 扩容 sv_*/mp_*/weapon_* 的前提）。
/// - 只有 **≥2 个真人** 时才启用（单机/本地人机只有房主 1 人 → 完全不拦截，自己开图不受限）。
/// - 拥有 <see cref="BypassPermission"/>（默认房主管理员 @css/root）永远放行。
/// - **仍不列** <c>bot_*</c> / <c>map</c> / <c>changelevel</c> / <c>kickid</c> / <c>banid</c>（开局/管理流程，
///   2026-09-14 曾因拦这些导致单机人机起不来），也不列普通丢枪指令 <c>drop</c>。
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
	/// 作弊指令 / 作弊 cvar 黑名单。服务器自己执行的命令不会命中（<c>player == null</c>），
	/// 所以 sv_*/mp_* 的服务器用途（cfg / 插件 ExecuteCommand / ReplicateConVar）安全。
	/// </summary>
	[JsonPropertyName("blocked_commands")]
	public List<string> BlockedCommands { get; set; } = new List<string>
	{
		// 移动/生存类作弊
		"noclip", "god", "buddha", "notarget", "hurtme",
		// 物品/武器（注意：不要加 "drop"——它是普通玩家的丢枪指令，不是作弊，会误伤）
		"give", "impulse",
		// 传送/位置
		"setpos", "setang", "setpos_exact", "setang_exact", "getpos",
		// 实体刷取/编辑
		"ent_create", "ent_remove", "ent_remove_all", "ent_fire", "ent_dump", "ent_info", "ent_text", "ent_teleport", "ent_bbox",
		// 视角
		"thirdperson",
		// cheats 总开关（防玩家关掉 cheats 让 dice 的无扩散/时间效果失效）
		"sv_cheats",
		// 移动/速度/弹药类作弊 cvar（bhop、加速、无限弹药——公开作弊配置里的常客）
		"sv_infinite_ammo", "sv_gravity", "sv_accelerate", "sv_airaccelerate",
		"sv_air_max_wishspeed", "sv_friction", "sv_staminajumpcost", "sv_staminalandcost",
		"sv_maxspeed", "sv_maxvelocity", "sv_enablebunnyhopping", "sv_autobunnyhopping",
		// 时间类（dice 的 Heaven/十六夜走 Server.ExecuteCommand，不受影响）
		"host_timescale", "phys_timescale",
		// 武器手感 cvar（dice 的无扩散走 ReplicateConVar，不受影响）
		"weapon_accuracy_nospread", "weapon_recoil_scale", "weapon_air_spread_scale",
		// 经济/购买类（公开练习配置的常客）
		"mp_buy_anywhere", "mp_buytime", "mp_freezetime", "mp_startmoney", "mp_maxmoney",
		// 回合/比赛流程防捣乱（换图、跳过热身、改回合时长/人数）
		"mp_roundtime", "mp_roundtime_defuse", "mp_maxrounds", "mp_restartgame",
		"mp_warmuptime", "mp_warmup_end", "mp_autoteambalance", "mp_limitteams",
		"mp_teammates_are_enemies", "mp_pause_match", "mp_unpause_match",
	};
}
