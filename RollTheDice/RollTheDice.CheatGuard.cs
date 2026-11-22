using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using RollTheDice.Configs;

namespace RollTheDice;

public partial class RollTheDice
{	/// <summary>
	/// CheatGuard 的每条被拦指令需要<em>各自独立</em>的委托实例。
	/// 原因：<c>BasePlugin.AddCommandListener</c> 内部用 <c>Dictionary&lt;Delegate,...&gt;</c> 以委托为键，
	/// 若所有被拦指令共用同一个 <c>OnCheatGuardCommand</c> 方法组委托，键会互相覆盖 →
	/// <c>RemoveCommandListener</c> 只能摘掉最后一个，其余原生钩子在卸载/热重载后泄漏并叠加。
	/// 每条指令包一个 <see cref="CheatGuardHook"/> 实例（Target 不同 → 委托不等）即可对称摘除。
	/// </summary>
	private sealed class CheatGuardHook
	{
		private readonly RollTheDice _plugin;

		public CheatGuardHook(RollTheDice plugin)
		{
			_plugin = plugin;
		}

		public HookResult Handle(CCSPlayerController? player, CommandInfo info)
		{
			return _plugin.OnCheatGuardCommand(player, info);
		}
	}

	private readonly List<(string Command, CommandInfo.CommandListenerCallback Handler)> _cheatGuardHooks = new List<(string, CommandInfo.CommandListenerCallback)>();

	private void RegisterCheatGuard()
	{
		CheatGuardConfig guard = Config?.CheatGuard;
		if (guard == null || !guard.Enabled || guard.BlockedCommands == null)
		{
			return;
		}
		foreach (string command in guard.BlockedCommands)
		{
			if (!string.IsNullOrWhiteSpace(command))
			{
				string name = command.Trim().ToLowerInvariant();
				CommandInfo.CommandListenerCallback handler = new CheatGuardHook(this).Handle;
				_cheatGuardHooks.Add((name, handler));
				((BasePlugin)this).AddCommandListener(name, handler, HookMode.Pre);
			}
		}
		LogDebug($"{DateTime.Now:HH:mm:ss} CheatGuard: blocked {guard.BlockedCommands.Count} commands (bypass={guard.BypassPermission})\n");
	}

	private void DeregisterCheatGuard()
	{
		foreach ((string command, CommandInfo.CommandListenerCallback handler) in _cheatGuardHooks)
		{
			((BasePlugin)this).RemoveCommandListener(command, handler, HookMode.Pre);
		}
		_cheatGuardHooks.Clear();
	}

	private HookResult OnCheatGuardCommand(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null || !((CEntityInstance)player).IsValid || player.IsBot || ((CBasePlayerController)player).IsHLTV)
		{
			return (HookResult)0;
		}
		CheatGuardConfig guard = Config?.CheatGuard;
		if (guard == null || !guard.Enabled)
		{
			return (HookResult)0;
		}
		if (!string.IsNullOrWhiteSpace(guard.BypassPermission) && AdminManager.PlayerHasPermissions(player, guard.BypassPermission))
		{
			return (HookResult)0;
		}
		int humanCount = Utilities.GetPlayers().Count((CCSPlayerController p) => p != null && ((CEntityInstance)p).IsValid && !p.IsBot && !((CBasePlayerController)p).IsHLTV);
		if (humanCount <= 1)
		{
			return (HookResult)0;
		}
		LogDebug($"{DateTime.Now:HH:mm:ss} CheatGuard blocked '{info.GetCommandString}' from {((CBasePlayerController)player).PlayerName}\n");
		try
		{
			player.PrintToChat($" {((BasePlugin)this).Localizer["command.prefix"].Value}⛔ 该作弊指令已被禁用（仅房主可用）。");
		}
		catch
		{
		}
		return (HookResult)3;
	}
}
