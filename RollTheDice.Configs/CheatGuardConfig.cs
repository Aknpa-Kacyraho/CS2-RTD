using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

/// <summary>
/// 作弊指令守卫：服务器 <c>sv_cheats 1</c> 时，拦掉非管理员使用服务器作弊指令/作弊 cvar，
/// 防止加入的玩家利用 cheats 开挂（noclip/god/give/impulse/setpos/ent_* 等）。
/// 注意：客户端本地 cvar（透视 r_drawothermodels、线框 mat_wireframe 等）服务器收不到，无法拦截。
/// </summary>
public class CheatGuardConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	/// <summary>拥有该权限的玩家（房主/管理员）不受限制，默认 CSS root。</summary>
	[JsonPropertyName("bypass_permission")]
	public string BypassPermission { get; set; } = "@css/root";

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
		// cheats 开关与作弊 cvar
		"sv_cheats", "sv_infinite_ammo", "sv_gravity", "sv_accelerate", "sv_airaccelerate",
		"sv_air_max_wishspeed", "sv_friction", "sv_staminajumpcost", "sv_staminalandcost",
		"sv_maxspeed", "sv_maxvelocity", "sv_enablebunnyhopping", "sv_autobunnyhopping", "sv_party_mode",
		// 时间类
		"host_timescale", "phys_timescale",
		// 武器手感 cvar（铁腕/天际用的无扩散由服务器自己 ReplicateConVar，不受影响；这里只挡玩家手动改）
		"weapon_accuracy_nospread", "weapon_recoil_scale", "weapon_air_spread_scale", "weapon_debug_spread_show",
		// bot 控制
		"bot_add", "bot_kick", "bot_stop", "bot_place", "bot_crouch", "bot_mimic",
		// 比赛/地图管理（防捣乱）
		"mp_restartgame", "mp_warmup_end", "mp_warmuptime", "mp_pause_match", "mp_unpause_match",
		"mp_roundtime", "mp_maxrounds", "mp_freezetime", "mp_buytime", "mp_startmoney",
		"mp_autoteambalance", "mp_limitteams",
		"changelevel", "map", "kickid", "banid",
		// 视角
		"thirdperson",
	};
}
