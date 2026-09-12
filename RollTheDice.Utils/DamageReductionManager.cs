using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// 伤害减免（跨 source 求和，同 source 可叠层）。实际应用已上移到主插件统一的 OnPlayerTakeDamagePre，
/// 各 dice 只负责注册/注销。
/// </summary>
public static class DamageReductionManager
{
	public static void Register(CCSPlayerController player, string source, float percentage, float? cap = null, float? durationSeconds = null)
	{
		StackingBonusManager.Set(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainReduction, source, percentage, cap, durationSeconds);
	}

	public static void RegisterBySteamId(ulong steamId, string source, float percentage, float? cap = null, float? durationSeconds = null)
	{
		StackingBonusManager.Set(steamId, StackingBonusManager.DomainReduction, source, percentage, cap, durationSeconds);
	}

	public static void AddStack(CCSPlayerController player, string source, float amount, float? cap = null, float? durationSeconds = null)
	{
		StackingBonusManager.AddStack(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainReduction, source, amount, cap, durationSeconds);
	}

	public static void Unregister(CCSPlayerController player, string source)
	{
		StackingBonusManager.Unregister(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainReduction, source);
	}

	public static void UnregisterBySteamId(ulong steamId, string source)
	{
		StackingBonusManager.Unregister(steamId, StackingBonusManager.DomainReduction, source);
	}

	public static float GetSource(CCSPlayerController player, string source)
	{
		return StackingBonusManager.GetSource(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainReduction, source);
	}

	public static float GetTotal(CCSPlayerController player)
	{
		return StackingBonusManager.GetTotal(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainReduction);
	}

	public static float GetTotalBySteamId(ulong steamId)
	{
		return StackingBonusManager.GetTotal(steamId, StackingBonusManager.DomainReduction);
	}

	/// <summary>已废弃：减伤改由主插件统一应用，这里恒为 0 以免旧 dice 代码重复减免。</summary>
	public static float GetEffective(CCSPlayerController player, float cap = 0.5f)
	{
		return 0f;
	}

	public static bool HasAny(CCSPlayerController player)
	{
		return StackingBonusManager.HasAny(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainReduction);
	}

	public static void ClearAll()
	{
		StackingBonusManager.ClearDomain(StackingBonusManager.DomainReduction);
	}
}
