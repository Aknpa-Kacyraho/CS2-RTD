using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using RollTheDice.Dices;

namespace RollTheDice.Utils;

public static class DynamicHandlers
{
	public static void RegisterModuleListener(BasePlugin basePlugin, string listenerName, DiceBlueprint plugin)
	{
		Type nestedType = typeof(Listeners).GetNestedType(listenerName);
		if (nestedType == null)
		{
			return;
		}
		MethodInfo method = plugin.GetType().GetMethod(listenerName);
		if (!(method == null))
		{
			Delegate obj = Delegate.CreateDelegate(nestedType, plugin, method);
			MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "RegisterListener" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
			if (!(methodInfo == null))
			{
				MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(nestedType);
				methodInfo2.Invoke(basePlugin, new object[1] { obj });
			}
		}
	}

	public static void DeregisterModuleListener(BasePlugin basePlugin, string listenerName, DiceBlueprint plugin)
	{
		Type nestedType = typeof(Listeners).GetNestedType(listenerName);
		if (nestedType == null)
		{
			return;
		}
		MethodInfo method = plugin.GetType().GetMethod(listenerName);
		if (!(method == null))
		{
			Delegate obj = Delegate.CreateDelegate(nestedType, plugin, method);
			MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "RemoveListener" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
			if (!(methodInfo == null))
			{
				MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(nestedType);
				methodInfo2.Invoke(basePlugin, new object[1] { obj });
			}
		}
	}

	public static void RegisterModuleEventHandler(BasePlugin basePlugin, string eventName, DiceBlueprint plugin)
	{
		Type type = typeof(BasePlugin).Assembly.GetType("CounterStrikeSharp.API.Core." + eventName);
		if (type == null)
		{
			return;
		}
		MethodInfo method = plugin.GetType().GetMethod(eventName);
		if (!(method == null))
		{
			Type type2 = typeof(BasePlugin).GetNestedType("GameEventHandler`1").MakeGenericType(type);
			Delegate obj = Delegate.CreateDelegate(type2, plugin, method);
			MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "RegisterEventHandler" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
			if (!(methodInfo == null))
			{
				MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(type);
				methodInfo2.Invoke(basePlugin, new object[2]
				{
					obj,
					(object)(HookMode)0
				});
			}
		}
	}

	public static void DeregisterModuleEventHandler(BasePlugin basePlugin, string eventName, DiceBlueprint module)
	{
		Type type = typeof(BasePlugin).Assembly.GetType("CounterStrikeSharp.API.Core." + eventName);
		if (type == null)
		{
			return;
		}
		MethodInfo method = module.GetType().GetMethod(eventName);
		if (!(method == null))
		{
			Type type2 = typeof(BasePlugin).GetNestedType("GameEventHandler`1").MakeGenericType(type);
			Delegate obj = Delegate.CreateDelegate(type2, module, method);
			MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "DeregisterEventHandler" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
			if (!(methodInfo == null))
			{
				MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(type);
				methodInfo2.Invoke(basePlugin, new object[2]
				{
					obj,
					(object)(HookMode)0
				});
			}
		}
	}

	public static void RegisterUserMessageHook(BasePlugin basePlugin, int messageId, DiceBlueprint plugin, HookMode hookMode)
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		MethodInfo method = plugin.GetType().GetMethod($"HookUserMessage{messageId}");
		if (method == null)
		{
			Console.WriteLine("[DynamicHandlers] Method not found for UserMessage ID: " + messageId);
			return;
		}
		Type typeFromHandle = typeof(UserMessage.UserMessageHandler);
		Delegate obj = Delegate.CreateDelegate(typeFromHandle, plugin, method);
		MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "HookUserMessage" && m.GetParameters().Length == 3);
		if (methodInfo == null)
		{
			Console.WriteLine("[DynamicHandlers] HookUserMessage method not found.");
			return;
		}
		methodInfo.Invoke(basePlugin, new object[3] { messageId, obj, hookMode });
	}

	public static void DeregisterUserMessageHook(BasePlugin basePlugin, int messageId, DiceBlueprint plugin, HookMode hookMode)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		MethodInfo method = plugin.GetType().GetMethod($"HookUserMessage{messageId}");
		if (!(method == null))
		{
		Type typeFromHandle = typeof(UserMessage.UserMessageHandler);
			Delegate obj = Delegate.CreateDelegate(typeFromHandle, plugin, method);
			MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "UnhookUserMessage" && m.GetParameters().Length == 3);
			if (!(methodInfo == null))
			{
				methodInfo.Invoke(basePlugin, new object[3] { messageId, obj, hookMode });
			}
		}
	}

	public static void RegisterCommand(BasePlugin basePlugin, string command, string description, DiceBlueprint plugin)
	{
		MethodInfo method = plugin.GetType().GetMethod("Command" + command.First().ToString().ToUpper(CultureInfo.CurrentCulture) + command.Substring(1, command.Length - 1));
		if (method == null)
		{
			Console.WriteLine("[DynamicHandlers] Method not found for command name: " + command);
			return;
		}
		MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "AddCommand" && m.GetParameters().Length == 3);
		if (methodInfo == null)
		{
			Console.WriteLine("[DynamicHandlers] AddCommand method not found.");
			return;
		}
		Type parameterType = methodInfo.GetParameters()[2].ParameterType;
		Delegate obj = Delegate.CreateDelegate(parameterType, plugin, method);
		methodInfo.Invoke(basePlugin, new object[3] { command, description, obj });
	}

	public static void DeregisterCommand(BasePlugin basePlugin, string command, DiceBlueprint plugin)
	{
		MethodInfo method = plugin.GetType().GetMethod("Command" + command.First().ToString().ToUpper(CultureInfo.CurrentCulture) + command.Substring(1, command.Length - 1));
		if (!(method == null))
		{
			MethodInfo methodInfo = typeof(BasePlugin).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "RemoveCommand" && m.GetParameters().Length == 3);
			if (!(methodInfo == null))
			{
				Type parameterType = methodInfo.GetParameters()[2].ParameterType;
				Delegate obj = Delegate.CreateDelegate(parameterType, plugin, method);
				methodInfo.Invoke(basePlugin, new object[2] { command, obj });
			}
		}
	}
}
