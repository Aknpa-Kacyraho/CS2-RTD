using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// 伤害加成（跨 source 求和，同 source 可叠层）。实际应用已上移到主插件统一的 OnPlayerTakeDamagePre，
/// 各 dice 只负责注册/注销，不要再各自乘 info.Damage。
/// </summary>
public static class DamageBonusManager
{
	public static void Register(CCSPlayerController player, string source, float percentage, float? cap = null)
	{
		StackingBonusManager.Set(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainDamage, source, percentage, cap);
	}

	public static void RegisterBySteamId(ulong steamId, string source, float percentage, float? cap = null)
	{
		StackingBonusManager.Set(steamId, StackingBonusManager.DomainDamage, source, percentage, cap);
	}

	public static void AddStack(CCSPlayerController player, string source, float amount, float? cap = null)
	{
		StackingBonusManager.AddStack(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainDamage, source, amount, cap);
	}

	public static void AddStackBySteamId(ulong steamId, string source, float amount, float? cap = null)
	{
		StackingBonusManager.AddStack(steamId, StackingBonusManager.DomainDamage, source, amount, cap);
	}

	public static void Unregister(CCSPlayerController player, string source)
	{
		StackingBonusManager.Unregister(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainDamage, source);
	}

	public static void UnregisterBySteamId(ulong steamId, string source)
	{
		StackingBonusManager.Unregister(steamId, StackingBonusManager.DomainDamage, source);
	}

	public static float GetSource(CCSPlayerController player, string source)
	{
		return StackingBonusManager.GetSource(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainDamage, source);
	}

	public static float GetTotal(CCSPlayerController player, float? cap = null)
	{
		return StackingBonusManager.GetTotal(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainDamage, cap);
	}

	public static float GetTotalBySteamId(ulong steamId, float? cap = null)
	{
		return StackingBonusManager.GetTotal(steamId, StackingBonusManager.DomainDamage, cap);
	}

	/// <summary>已废弃：伤害加成改由主插件统一应用，这里恒为 0 以免旧 dice 代码重复放大。</summary>
	public static float GetEffective(CCSPlayerController player, float cap = 0.5f)
	{
		return 0f;
	}

	public static bool IsHighest(CCSPlayerController player, string source)
	{
		float own = GetSource(player, source);
		return own != 0f && own >= GetTotal(player);
	}

	public static bool HasAny(CCSPlayerController player)
	{
		return StackingBonusManager.HasAny(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainDamage);
	}

	public static void ClearAll()
	{
		StackingBonusManager.ClearDomain(StackingBonusManager.DomainDamage);
	}
}
