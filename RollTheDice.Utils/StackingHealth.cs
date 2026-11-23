using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Utils;

public static class StackingHealth
{
	private static readonly Dictionary<ulong, Dictionary<string, float>> _modifiers = new Dictionary<ulong, Dictionary<string, float>>();

	private static readonly Dictionary<ulong, int> _baseHealth = new Dictionary<ulong, int>();

	public static void RegisterMultiplier(CCSPlayerController player, string key, float multiplier)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			ulong steamID = ((CBasePlayerController)player).SteamID;
			if (!_modifiers.ContainsKey(steamID))
			{
				_modifiers[steamID] = new Dictionary<string, float>();
			}
			if (!_baseHealth.ContainsKey(steamID))
			{
				// Capture the real base health instead of assuming 100; other plugins
				// or server modes may use a different value.
				_baseHealth[steamID] = ((CBaseEntity)player.PlayerPawn.Value).MaxHealth;
			}
			if (!_modifiers[steamID].ContainsKey(key))
			{
				_modifiers[steamID][key] = multiplier;
				Recompute(player);
			}
		}
	}

	public static void UnregisterMultiplier(CCSPlayerController player, string key)
	{
		if (player == null)
		{
			return;
		}
		if (!player.IsValid)
		{
			// No safe SteamID read on an invalid controller. Disconnect cleanup uses
			// Clear(ulong) instead of guessing key 0.
			return;
		}
		ulong steamID = ((CBasePlayerController)player).SteamID;
		if (!_modifiers.TryGetValue(steamID, out Dictionary<string, float> value))
		{
			return;
		}
		value.Remove(key);
		if (value.Count == 0)
		{
			int baseHealth = _baseHealth.TryGetValue(steamID, out int storedBase) ? storedBase : 100;
			_modifiers.Remove(steamID);
			_baseHealth.Remove(steamID);
			if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value2 = player.PlayerPawn.Value;
				((CBaseEntity)value2).MaxHealth = baseHealth;
				if (((CBaseEntity)value2).Health > baseHealth)
				{
					((CBaseEntity)value2).Health = baseHealth;
				}
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			}
		}
		else
		{
			Recompute(player);
		}
	}

	public static void Recompute(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		CCSPlayerPawn value = player.PlayerPawn.Value;
		ulong steamID = ((CBasePlayerController)player).SteamID;
		float num = 1f;
		if (_modifiers.TryGetValue(steamID, out Dictionary<string, float> value2))
		{
			foreach (float value4 in value2.Values)
			{
				num *= value4;
			}
		}
		int num2 = (_baseHealth.TryGetValue(steamID, out var value3) ? value3 : 100);
		int num3 = (int)float.Round((float)num2 * num);
		((CBaseEntity)value).MaxHealth = num3;
		if (((CBaseEntity)value).Health > num3)
		{
			((CBaseEntity)value).Health = num3;
		}
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
	}

	public static void Clear(ulong steamID)
	{
		_modifiers.Remove(steamID);
		_baseHealth.Remove(steamID);
	}

	/// <summary>Cleanup-only overload for invalid/disconnected controllers (no pawn recompute).</summary>
	public static void UnregisterMultiplier(ulong steamId, string key)
	{
		if (!_modifiers.TryGetValue(steamId, out Dictionary<string, float> map))
		{
			return;
		}
		map.Remove(key);
		if (map.Count == 0)
		{
			_modifiers.Remove(steamId);
			_baseHealth.Remove(steamId);
		}
	}

	public static void ClearAll()
	{
		_modifiers.Clear();
		_baseHealth.Clear();
	}
}
