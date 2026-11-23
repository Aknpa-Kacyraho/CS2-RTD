using System;
using System.Collections.Generic;

namespace RollTheDice.Utils;

/// <summary>
/// Pure movement-scope stack: each key owns a target MoveType, the last scope
/// wins, and removing the last scope tells the caller to restore the original.
/// Kept free of native types so the nesting rules can be unit-tested.
/// </summary>
public sealed class MoveScopeStack
{
	private readonly Dictionary<string, int> _targets = new Dictionary<string, int>(StringComparer.Ordinal);
	private readonly List<string> _order = new List<string>();

	public int Count => _order.Count;

	public int? CurrentTarget => _order.Count == 0 ? null : _targets[_order[_order.Count - 1]];

	public bool Contains(string key)
	{
		return !string.IsNullOrEmpty(key) && _targets.ContainsKey(key);
	}

	/// <summary>Add or update a scope without changing an existing scope's priority.</summary>
	public int Push(string key, int target)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("Movement scope key is required.", nameof(key));
		}
		if (!_targets.ContainsKey(key))
		{
			_order.Add(key);
		}
		_targets[key] = target;
		return _targets[_order[_order.Count - 1]];
	}

	/// <summary>
	/// Remove a scope. <paramref name="currentTarget"/> is null when the stack is
	/// empty and the caller should restore the captured original MoveType.
	/// </summary>
	public bool Remove(string key, out int? currentTarget)
	{
		currentTarget = CurrentTarget;
		if (string.IsNullOrEmpty(key) || !_targets.Remove(key))
		{
			return false;
		}
		_order.Remove(key);
		currentTarget = CurrentTarget;
		return true;
	}

	public void Clear()
	{
		_targets.Clear();
		_order.Clear();
	}
}
