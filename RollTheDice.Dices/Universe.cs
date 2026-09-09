using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Universe : DiceBlueprint
{
	public static readonly Dictionary<ulong, int> RestoresLeft = new Dictionary<ulong, int>();

	public static readonly HashSet<ulong> PendingRestore = new HashSet<ulong>();

	public override string ClassName => "Universe";

	public override bool CanBeDrawn => false;

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

	public Universe(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			int maxRestores = _config.Dices.Universe.MaxRestores;
			if (!RestoresLeft.ContainsKey(((CBasePlayerController)player).SteamID))
			{
				RestoresLeft[((CBasePlayerController)player).SteamID] = maxRestores;
				player.PrintToCenterAlert($"\ud83c\udf0c 宇宙之力！死亡时自动回溯，剩余{maxRestores}次");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"max",
					maxRestores.ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
	}

	public override void Destroy()
	{
		RestoresLeft.Clear();
		PendingRestore.Clear();
		_players.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		if (!RestoresLeft.TryGetValue(((CBasePlayerController)userid).SteamID, out var value) || value <= 0)
		{
			return (HookResult)0;
		}
		string fileName = RollTheDice.GetRoundBackupFile();
		if (string.IsNullOrEmpty(fileName))
		{
			return (HookResult)0;
		}
		int value2 = value - 1;
		RestoresLeft[((CBasePlayerController)userid).SteamID] = value2;
		PendingRestore.Add(((CBasePlayerController)userid).SteamID);
		string playerName = ((CBasePlayerController)userid).PlayerName;
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Universe_trigger"].Value.Replace("{playerName}", playerName).Replace("{left}", value2.ToString()));
		Server.NextFrame((Action)delegate
		{
			Server.ExecuteCommand("mp_backup_restore_load_file " + fileName);
		});
		return (HookResult)0;
	}
}
