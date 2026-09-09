using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

public static class DamageReductionManager
{
	private const float DefaultCap = 0.5f;

	private static readonly Dictionary<ulong, Dictionary<string, float>> _reductions = new Dictionary<ulong, Dictionary<string, float>>();

	public static void Register(CCSPlayerController player, string source, float percentage)
	{
		RegisterBySteamId(((CBasePlayerController)player).SteamID, source, percentage);
	}

	public static void RegisterBySteamId(ulong steamId, string source, float percentage)
	{
		if (!_reductions.ContainsKey(steamId))
		{
			_reductions[steamId] = new Dictionary<string, float>();
		}
		_reductions[steamId][source] = percentage;
	}

	public static void Unregister(CCSPlayerController player, string source)
	{
		UnregisterBySteamId(((CBasePlayerController)player).SteamID, source);
	}

	public static void UnregisterBySteamId(ulong steamId, string source)
	{
		if (_reductions.TryGetValue(steamId, out Dictionary<string, float> value))
		{
			value.Remove(source);
			if (value.Count == 0)
			{
				_reductions.Remove(steamId);
			}
		}
	}

	public static float GetEffective(CCSPlayerController player, float cap = 0.5f)
	{
		return GetEffectiveBySteamId(((CBasePlayerController)player).SteamID, cap);
	}

	public static float GetEffectiveBySteamId(ulong steamId, float cap = 0.5f)
	{
		if (_reductions.TryGetValue(steamId, out Dictionary<string, float> value) && value.Count > 0)
		{
			return Math.Min(value.Values.Max(), cap);
		}
		return 0f;
	}

	public static bool HasAny(CCSPlayerController player)
	{
		Dictionary<string, float> value;
		return _reductions.TryGetValue(((CBasePlayerController)player).SteamID, out value) && value.Count > 0;
	}

	public static void ClearAll()
	{
		_reductions.Clear();
	}
}
