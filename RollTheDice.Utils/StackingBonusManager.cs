using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;

namespace RollTheDice.Utils;

/// <summary>
/// 统一的可叠加 buff 管理器（按 domain 隔离：dmg / spd / red，避免不同类别的 source 混算）。
/// 规则：同一 (domain, source) 反复 AddStack = 叠层（受该 source 可选 cap 限制，cap=null 无上限）；
///       跨 source = 各 source 先按各自 cap 截断，再求和；可选 duration 到期失效。
/// </summary>
public static class StackingBonusManager
{
	public const string DomainDamage = "dmg";
	public const string DomainSpeed = "spd";
	public const string DomainReduction = "red";

	private sealed class Entry
	{
		public float Amount;
		public float? Cap;
		public float? ExpireAt;
	}

	private static readonly Dictionary<ulong, Dictionary<string, Entry>> _bonuses = new Dictionary<ulong, Dictionary<string, Entry>>();

	public static void Set(ulong steamId, string domain, string source, float amount, float? cap = null, float? durationSeconds = null)
	{
		Dictionary<string, Entry> map = GetMap(steamId);
		map[Key(domain, source)] = new Entry
		{
			Amount = amount,
			Cap = cap,
			ExpireAt = durationSeconds.HasValue ? (Server.CurrentTime + durationSeconds.Value) : (float?)null
		};
	}

	public static void AddStack(ulong steamId, string domain, string source, float amount, float? cap = null, float? durationSeconds = null)
	{
		Dictionary<string, Entry> map = GetMap(steamId);
		string key = Key(domain, source);
		if (!map.TryGetValue(key, out Entry entry) || IsExpired(entry))
		{
			map[key] = new Entry
			{
				Amount = amount,
				Cap = cap,
				ExpireAt = durationSeconds.HasValue ? (Server.CurrentTime + durationSeconds.Value) : (float?)null
			};
			return;
		}
		entry.Amount += amount;
		if (cap.HasValue)
		{
			entry.Cap = cap;
		}
		if (durationSeconds.HasValue)
		{
			entry.ExpireAt = Server.CurrentTime + durationSeconds.Value;
		}
	}

	public static void Unregister(ulong steamId, string domain, string source)
	{
		if (_bonuses.TryGetValue(steamId, out Dictionary<string, Entry> map))
		{
			map.Remove(Key(domain, source));
			if (map.Count == 0)
			{
				_bonuses.Remove(steamId);
			}
		}
	}

	public static float GetSource(ulong steamId, string domain, string source)
	{
		if (_bonuses.TryGetValue(steamId, out Dictionary<string, Entry> map) && map.TryGetValue(Key(domain, source), out Entry entry) && !IsExpired(entry))
		{
			return Clamp(entry);
		}
		return 0f;
	}

	public static float GetTotal(ulong steamId, string domain, float? cap = null)
	{
		if (!_bonuses.TryGetValue(steamId, out Dictionary<string, Entry> map) || map.Count == 0)
		{
			return 0f;
		}
		string prefix = domain + ":";
		float total = 0f;
		foreach (KeyValuePair<string, Entry> kv in map)
		{
			if (kv.Key.StartsWith(prefix) && !IsExpired(kv.Value))
			{
				total += Clamp(kv.Value);
			}
		}
		if (cap.HasValue && total > cap.Value)
		{
			total = cap.Value;
		}
		return total;
	}

	public static bool HasAny(ulong steamId, string domain)
	{
		if (!_bonuses.TryGetValue(steamId, out Dictionary<string, Entry> map))
		{
			return false;
		}
		string prefix = domain + ":";
		return map.Any((kv) => kv.Key.StartsWith(prefix) && !IsExpired(kv.Value));
	}

	public static void ClearDomain(string domain)
	{
		string prefix = domain + ":";
		foreach (ulong key in _bonuses.Keys.ToList())
		{
			Dictionary<string, Entry> map = _bonuses[key];
			foreach (string k in map.Keys.Where((k) => k.StartsWith(prefix)).ToList())
			{
				map.Remove(k);
			}
			if (map.Count == 0)
			{
				_bonuses.Remove(key);
			}
		}
	}

	public static void ClearAll()
	{
		_bonuses.Clear();
	}

	public static void PruneExpired()
	{
		foreach (ulong key in _bonuses.Keys.ToList())
		{
			Dictionary<string, Entry> map = _bonuses[key];
			foreach (string source in map.Keys.ToList())
			{
				if (IsExpired(map[source]))
				{
					map.Remove(source);
				}
			}
			if (map.Count == 0)
			{
				_bonuses.Remove(key);
			}
		}
	}

	private static Dictionary<string, Entry> GetMap(ulong steamId)
	{
		if (!_bonuses.TryGetValue(steamId, out Dictionary<string, Entry> map))
		{
			map = new Dictionary<string, Entry>();
			_bonuses[steamId] = map;
		}
		return map;
	}

	private static string Key(string domain, string source)
	{
		return domain + ":" + source;
	}

	private static bool IsExpired(Entry entry)
	{
		return entry.ExpireAt.HasValue && Server.CurrentTime >= entry.ExpireAt.Value;
	}

	private static float Clamp(Entry entry)
	{
		return (entry.Cap.HasValue && entry.Amount > entry.Cap.Value) ? entry.Cap.Value : entry.Amount;
	}
}
