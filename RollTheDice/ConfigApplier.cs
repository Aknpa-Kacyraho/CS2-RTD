using System;
using System.Collections.Generic;
using System.Reflection;

namespace RollTheDice;

/// <summary>
/// Applies a fully deserialized config to the live instance with rollback.
/// CounterStrikeSharp's Reload deserializes before copying, but its property
/// loop has no rollback if a setter throws halfway through. This helper makes
/// the JSON reload all-or-nothing while keeping the same object identity CSS
/// already holds.
/// </summary>
public static class ConfigApplier
{
	public static void Apply<T>(T target, T source) where T : class
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target));
		}
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		List<PropertyInfo> writable = new List<PropertyInfo>();
		List<object?> snapshot = new List<object?>();
		foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
			{
				continue;
			}
			writable.Add(property);
			snapshot.Add(property.GetValue(target));
		}

		int applied = 0;
		try
		{
			for (; applied < writable.Count; applied++)
			{
				PropertyInfo property = writable[applied];
				property.SetValue(target, property.GetValue(source));
			}
		}
		catch (Exception ex)
		{
			for (int i = 0; i < applied; i++)
			{
				try
				{
					writable[i].SetValue(target, snapshot[i]);
				}
				catch
				{
				}
			}
			if (ex is TargetInvocationException && ex.InnerException != null)
			{
				throw ex.InnerException;
			}
			throw;
		}
	}
}
