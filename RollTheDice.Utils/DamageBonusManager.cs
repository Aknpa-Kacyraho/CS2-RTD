using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

public static class DamageBonusManager
{
	private const float DefaultCap = 0.5f;

	private static readonly Dictionary<ulong, Dictionary<string, float>> _bonuses = new Dictionary<ulong, Dictionary<string, float>>();

	public static void Register(CCSPlayerController player, string source, float percentage)
	{
		RegisterBySteamId(((CBasePlayerController)player).SteamID, source, percentage);
	}

	public static void RegisterBySteamId(ulong steamId, string source, float percentage)
	{
		if (!_bonuses.ContainsKey(steamId))
		{
			_bonuses[steamId] = new Dictionary<string, float>();
		}
		_bonuses[steamId][source] = percentage;
	}

	public static void Unregister(CCSPlayerController player, string source)
	{
		UnregisterBySteamId(((CBasePlayerController)player).SteamID, source);
	}

	public static void UnregisterBySteamId(ulong steamId, string source)
	{
		if (_bonuses.TryGetValue(steamId, out Dictionary<string, float> value))
		{
			value.Remove(source);
			if (value.Count == 0)
			{
				_bonuses.Remove(steamId);
			}
		}
	}

	public static float GetEffective(CCSPlayerController player, float cap = 0.5f)
	{
		return GetEffectiveBySteamId(((CBasePlayerController)player).SteamID, cap);
	}

	public static float GetEffectiveBySteamId(ulong steamId, float cap = 0.5f)
	{
		if (_bonuses.TryGetValue(steamId, out Dictionary<string, float> value) && value.Count > 0)
		{
			return Math.Min(value.Values.Max(), cap);
		}
		return 0f;
	}

	public static bool IsHighest(CCSPlayerController player, string source)
	{
		if (!_bonuses.TryGetValue(((CBasePlayerController)player).SteamID, out Dictionary<string, float> value) || value.Count == 0)
		{
			return false;
		}
		if (!value.TryGetValue(source, out var value2))
		{
			return false;
		}
		float num = value.Values.Max();
		return MathF.Abs(value2 - num) < 0.001f;
	}

	public static bool HasAny(CCSPlayerController player)
	{
		Dictionary<string, float> value;
		return _bonuses.TryGetValue(((CBasePlayerController)player).SteamID, out value) && value.Count > 0;
	}

	public static void ClearAll()
	{
		_bonuses.Clear();
	}
}
