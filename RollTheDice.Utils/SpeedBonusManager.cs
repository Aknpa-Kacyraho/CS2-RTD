using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

/// <summary>
/// 移速加成（跨 source 求和，同 source 可叠层）。读取方（各 dice 的 OnTick）用 VelocityModifier 写入，
/// 多来源不会重复叠加，求和即可。
/// </summary>
public static class SpeedBonusManager
{
	public static void Register(CCSPlayerController player, string source, float percentage)
	{
		StackingBonusManager.Set(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainSpeed, source, percentage);
	}

	public static void RegisterBySteamId(ulong steamId, string source, float percentage)
	{
		StackingBonusManager.Set(steamId, StackingBonusManager.DomainSpeed, source, percentage);
	}

	public static void AddStack(CCSPlayerController player, string source, float amount, float? cap = null)
	{
		StackingBonusManager.AddStack(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainSpeed, source, amount, cap);
	}

	public static void Unregister(CCSPlayerController player, string source)
	{
		StackingBonusManager.Unregister(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainSpeed, source);
	}

	public static void UnregisterBySteamId(ulong steamId, string source)
	{
		StackingBonusManager.Unregister(steamId, StackingBonusManager.DomainSpeed, source);
	}

	public static float GetEffective(CCSPlayerController player, float cap = 100f)
	{
		return StackingBonusManager.GetTotal(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainSpeed, cap);
	}

	public static float GetEffectiveBySteamId(ulong steamId, float cap = 100f)
	{
		return StackingBonusManager.GetTotal(steamId, StackingBonusManager.DomainSpeed, cap);
	}

	public static float GetTotal(CCSPlayerController player, float? cap = null)
	{
		return StackingBonusManager.GetTotal(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainSpeed, cap);
	}

	public static bool HasAny(CCSPlayerController player)
	{
		return StackingBonusManager.HasAny(((CBasePlayerController)player).SteamID, StackingBonusManager.DomainSpeed);
	}

	public static void ClearAll()
	{
		StackingBonusManager.ClearDomain(StackingBonusManager.DomainSpeed);
	}
}
